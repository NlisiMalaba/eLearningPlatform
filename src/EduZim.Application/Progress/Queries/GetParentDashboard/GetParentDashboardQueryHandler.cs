using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Progress.Queries.GetParentDashboard;

public sealed class GetParentDashboardQueryHandler : IRequestHandler<GetParentDashboardQuery, ParentDashboardDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetParentDashboardQueryHandler> _logger;

    public GetParentDashboardQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetParentDashboardQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ParentDashboardDto> Handle(GetParentDashboardQuery request, CancellationToken ct)
    {
        ParentDashboardAccess.EnsureCanView(_currentUser, request.TenantId, request.ParentId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        List<Guid> studentIds = await LoadLinkedStudentIdsAsync(request, ct).ConfigureAwait(false);
        List<Module> modules = await LoadModulesAsync(request.TenantId, ct).ConfigureAwait(false);
        DashboardSnapshot snapshot = await LoadSnapshotAsync(request.TenantId, studentIds, ct)
            .ConfigureAwait(false);

        IReadOnlyList<LinkedStudentDashboardDto> students = studentIds
            .Select(id => MapStudent(id, modules, snapshot))
            .ToList();

        _logger.LogDebug(
            "Parent dashboard queried for parent {ParentId}: {StudentCount} linked student(s).",
            request.ParentId,
            students.Count);
        return new ParentDashboardDto(request.ParentId, students);
    }

    private async Task<List<Guid>> LoadLinkedStudentIdsAsync(GetParentDashboardQuery request, CancellationToken ct)
    {
        return await _db.ParentStudentLinks
            .AsNoTracking()
            .Where(l => l.TenantId == request.TenantId && l.ParentUserId == request.ParentId)
            .Select(l => l.StudentUserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<List<Module>> LoadModulesAsync(Guid tenantId, CancellationToken ct)
    {
        return await _db.Modules
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<DashboardSnapshot> LoadSnapshotAsync(
        Guid tenantId,
        List<Guid> studentIds,
        CancellationToken ct)
    {
        if (studentIds.Count == 0)
            return new DashboardSnapshot([], []);

        List<StudentProgress> progress = await _db.StudentProgresses
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && studentIds.Contains(p.StudentId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<Badge> badges = await _db.Badges
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && studentIds.Contains(b.StudentId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new DashboardSnapshot(progress, badges);
    }

    private static LinkedStudentDashboardDto MapStudent(
        Guid studentId,
        List<Module> modules,
        DashboardSnapshot snapshot)
    {
        HashSet<Guid> completedIds = snapshot.Progress
            .Where(p => p.StudentId == studentId && p.IsCompleted)
            .Select(p => p.ModuleId)
            .ToHashSet();
        List<ParentDashboardRules.ModuleGradeRow> rows = modules
            .Select(m => new ParentDashboardRules.ModuleGradeRow(m.Id, m.Grade, m.Subject))
            .ToList();
        GradeLevel currentGrade = ParentDashboardRules.ResolveCurrentGrade(rows, completedIds);
        IReadOnlyList<string> subjects = ParentDashboardRules.SubjectsForGrade(rows, currentGrade);
        int overall = ProgressPercentageRules.Calculate(completedIds.Count, modules.Count);
        IReadOnlyList<RecentActivityDto> recent = RecentActivityMapper.Build(
            studentId,
            modules,
            snapshot.Progress,
            snapshot.Badges);
        return new LinkedStudentDashboardDto(studentId, currentGrade, subjects, recent, overall);
    }

    private sealed record DashboardSnapshot(
        IReadOnlyList<StudentProgress> Progress,
        IReadOnlyList<Badge> Badges);
}
