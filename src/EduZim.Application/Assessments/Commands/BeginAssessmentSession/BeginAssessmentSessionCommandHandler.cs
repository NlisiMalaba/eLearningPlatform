using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Assessments.Commands.BeginAssessmentSession;

public sealed class BeginAssessmentSessionCommandHandler
    : IRequestHandler<BeginAssessmentSessionCommand, BeginAssessmentSessionResultDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAssessmentBackgroundJobs _backgroundJobs;
    private readonly ILogger<BeginAssessmentSessionCommandHandler> _logger;

    public BeginAssessmentSessionCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IAssessmentBackgroundJobs backgroundJobs,
        ILogger<BeginAssessmentSessionCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task<BeginAssessmentSessionResultDto> Handle(
        BeginAssessmentSessionCommand request,
        CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanAccessTenantScope(_currentUser, request.TenantId);
        EnsureStudentRole(request.TenantId);

        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        await EnsureEnrolledAsync(request, ct).ConfigureAwait(false);

        AssessmentClassAssignment assignment = await FindAssignmentAsync(request, ct).ConfigureAwait(false);
        Assessment assessment = await LoadAssessmentAsync(request, ct).ConfigureAwait(false);
        await EnsureNoOpenAttemptAsync(request, ct).ConfigureAwait(false);

        BeginAssessmentSessionResultDto result = await CreateAttemptAsync(request, assignment, assessment, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Started assessment attempt {AttemptId} for assessment {AssessmentId}.",
            result.AttemptId,
            request.AssessmentId);

        return result;
    }

    private void EnsureStudentRole(Guid tenantId)
    {
        if (_currentUser.Role != UserRole.Student)
        {
            throw new TenantAccessViolationException(
                "Only students can start an assessment session.",
                tenantId,
                null);
        }
    }

    private async Task EnsureEnrolledAsync(BeginAssessmentSessionCommand request, CancellationToken ct)
    {
        bool enrolled = await _db.ClassEnrollments.AnyAsync(
                e => e.SchoolClassId == request.SchoolClassId
                    && e.StudentUserId == _currentUser.UserId
                    && e.TenantId == request.TenantId,
                ct)
            .ConfigureAwait(false);
        if (!enrolled)
        {
            throw new TenantAccessViolationException(
                "You are not enrolled in this class.",
                request.TenantId,
                request.SchoolClassId);
        }
    }

    private async Task<AssessmentClassAssignment> FindAssignmentAsync(
        BeginAssessmentSessionCommand request,
        CancellationToken ct)
    {
        AssessmentClassAssignment? assignment = await _db.AssessmentClassAssignments
            .FirstOrDefaultAsync(
                a => a.AssessmentId == request.AssessmentId
                    && a.SchoolClassId == request.SchoolClassId
                    && a.TenantId == request.TenantId,
                ct)
            .ConfigureAwait(false);
        if (assignment is null)
            throw new NotFoundException(nameof(AssessmentClassAssignment), request.AssessmentId);

        return assignment;
    }

    private async Task<Assessment> LoadAssessmentAsync(BeginAssessmentSessionCommand request, CancellationToken ct)
    {
        Assessment? assessment = await _db.Assessments
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId && a.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (assessment is null)
            throw new NotFoundException(nameof(Assessment), request.AssessmentId);

        return assessment;
    }

    private async Task EnsureNoOpenAttemptAsync(BeginAssessmentSessionCommand request, CancellationToken ct)
    {
        bool hasOpenAttempt = await _db.AssessmentAttempts.AnyAsync(
                a => a.AssessmentId == request.AssessmentId
                    && a.StudentId == _currentUser.UserId
                    && a.SubmittedAt == null
                    && a.TenantId == request.TenantId,
                ct)
            .ConfigureAwait(false);
        if (hasOpenAttempt)
            throw new ConflictException("An assessment attempt is already in progress.");
    }

    private async Task<BeginAssessmentSessionResultDto> CreateAttemptAsync(
        BeginAssessmentSessionCommand request,
        AssessmentClassAssignment assignment,
        Assessment assessment,
        CancellationToken ct)
    {
        DateTime startedAt = DateTime.UtcNow;
        Guid attemptId = Guid.NewGuid();
        DateTime now = startedAt;
        var attempt = new AssessmentAttempt
        {
            Id = attemptId,
            TenantId = request.TenantId,
            AssessmentId = request.AssessmentId,
            StudentId = _currentUser.UserId,
            AssessmentClassAssignmentId = assignment.Id,
            StartedAt = startedAt,
            ScorePercent = 0,
            TimeTakenSeconds = 0,
            SubmittedAt = null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (assessment.TimeLimitSeconds is int limit && limit > 0)
        {
            DateTime runAtUtc = startedAt.AddSeconds(limit);
            attempt.TimedAutoSubmitHangfireJobId =
                _backgroundJobs.ScheduleTimedAutoSubmitAt(request.TenantId, attemptId, runAtUtc);
        }

        await _db.AssessmentAttempts.AddAsync(attempt, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new BeginAssessmentSessionResultDto(attemptId);
    }
}
