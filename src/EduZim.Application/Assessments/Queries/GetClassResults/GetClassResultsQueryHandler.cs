using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Assessments.Queries.GetClassResults;

public sealed class GetClassResultsQueryHandler : IRequestHandler<GetClassResultsQuery, ClassAssessmentResultsDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetClassResultsQueryHandler> _logger;

    public GetClassResultsQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetClassResultsQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ClassAssessmentResultsDto> Handle(GetClassResultsQuery request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanViewTenantDashboard(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        await ValidateAssessmentAndAssignmentAsync(request, ct).ConfigureAwait(false);

        SchoolClass schoolClass = await LoadSchoolClassAsync(request, ct).ConfigureAwait(false);
        List<Guid> studentIds = await LoadEnrolledStudentIdsAsync(request, ct).ConfigureAwait(false);
        Dictionary<Guid, AssessmentAttempt> latestByStudent = await LoadLatestAttemptsAsync(request, studentIds, ct)
            .ConfigureAwait(false);

        IReadOnlyList<StudentAssessmentResultRowDto> rows = BuildStudentRows(studentIds, latestByStudent);
        (int completionPercent, double? avgTime) = ComputeAggregates(rows, studentIds.Count);

        _logger.LogDebug(
            "Class results for assessment {AssessmentId}, class {ClassId}: {Completed}/{Enrolled} completed.",
            request.AssessmentId,
            request.SchoolClassId,
            rows.Count(r => r.HasCompleted),
            studentIds.Count);

        return new ClassAssessmentResultsDto(
            request.SchoolClassId,
            schoolClass.Name,
            studentIds.Count,
            rows.Count(r => r.HasCompleted),
            completionPercent,
            avgTime,
            rows);
    }

    private async Task ValidateAssessmentAndAssignmentAsync(GetClassResultsQuery request, CancellationToken ct)
    {
        Assessment? assessment = await _db.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId && a.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (assessment is null)
            throw new NotFoundException(nameof(Assessment), request.AssessmentId);

        bool assigned = await _db.AssessmentClassAssignments.AnyAsync(
                a => a.AssessmentId == request.AssessmentId
                    && a.SchoolClassId == request.SchoolClassId
                    && a.TenantId == request.TenantId,
                ct)
            .ConfigureAwait(false);
        if (!assigned)
            throw new NotFoundException(nameof(AssessmentClassAssignment), request.AssessmentId);
    }

    private async Task<SchoolClass> LoadSchoolClassAsync(GetClassResultsQuery request, CancellationToken ct)
    {
        SchoolClass? schoolClass = await _db.SchoolClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.SchoolClassId && c.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (schoolClass is null)
            throw new NotFoundException(nameof(SchoolClass), request.SchoolClassId);

        return schoolClass;
    }

    private async Task<List<Guid>> LoadEnrolledStudentIdsAsync(GetClassResultsQuery request, CancellationToken ct)
    {
        return await _db.ClassEnrollments
            .AsNoTracking()
            .Where(e => e.SchoolClassId == request.SchoolClassId && e.TenantId == request.TenantId)
            .Select(e => e.StudentUserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<Dictionary<Guid, AssessmentAttempt>> LoadLatestAttemptsAsync(
        GetClassResultsQuery request,
        List<Guid> studentIds,
        CancellationToken ct)
    {
        if (studentIds.Count == 0)
            return new Dictionary<Guid, AssessmentAttempt>();

        List<AssessmentAttempt> attempts = await _db.AssessmentAttempts
            .AsNoTracking()
            .Where(
                a => a.AssessmentId == request.AssessmentId
                    && a.TenantId == request.TenantId
                    && a.SubmittedAt != null
                    && studentIds.Contains(a.StudentId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return attempts
            .GroupBy(a => a.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.SubmittedAt!.Value).First());
    }

    private static IReadOnlyList<StudentAssessmentResultRowDto> BuildStudentRows(
        List<Guid> studentIds,
        Dictionary<Guid, AssessmentAttempt> latestByStudent)
    {
        var rows = new List<StudentAssessmentResultRowDto>();
        foreach (Guid sid in studentIds)
        {
            if (latestByStudent.TryGetValue(sid, out AssessmentAttempt? att))
            {
                rows.Add(
                    new StudentAssessmentResultRowDto(
                        sid,
                        att.ScorePercent,
                        HasCompleted: true,
                        att.TimeTakenSeconds));
            }
            else
            {
                rows.Add(new StudentAssessmentResultRowDto(sid, null, HasCompleted: false, null));
            }
        }

        return rows;
    }

    private static (int CompletionPercent, double? AverageTime) ComputeAggregates(
        IReadOnlyList<StudentAssessmentResultRowDto> rows,
        int enrolled)
    {
        int completed = rows.Count(r => r.HasCompleted);
        int completionPercent = enrolled == 0
            ? 0
            : (int)Math.Round(100.0 * completed / enrolled, MidpointRounding.AwayFromZero);

        List<int> times = rows.Where(r => r.TimeTakenSeconds.HasValue).Select(r => r.TimeTakenSeconds!.Value).ToList();
        double? avgTime = times.Count == 0 ? null : times.Average(t => (double)t);

        return (completionPercent, avgTime);
    }
}
