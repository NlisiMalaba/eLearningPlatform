using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Gamification.Notifications;

/// <summary>Awards milestone badges and queues certificate generation.</summary>
public sealed class CheckAndAwardBadgesCommandHandler :
    INotificationHandler<ModuleCompletedNotification>,
    INotificationHandler<AssessmentSubmittedNotification>
{
    private readonly IEduZimDbContext _db;
    private readonly IPublisher _publisher;
    private readonly IGamificationBackgroundJobs _backgroundJobs;
    private readonly ILogger<CheckAndAwardBadgesCommandHandler> _logger;

    public CheckAndAwardBadgesCommandHandler(
        IEduZimDbContext db,
        IPublisher publisher,
        IGamificationBackgroundJobs backgroundJobs,
        ILogger<CheckAndAwardBadgesCommandHandler> logger)
    {
        _db = db;
        _publisher = publisher;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public Task Handle(ModuleCompletedNotification notification, CancellationToken ct) =>
        EvaluateAndAwardAsync(notification.TenantId, notification.StudentId, notification.ModuleId, ct);

    public Task Handle(AssessmentSubmittedNotification notification, CancellationToken ct) =>
        EvaluateAndAwardAsync(notification.TenantId, notification.StudentId, triggeringModuleId: null, ct);

    private async Task EvaluateAndAwardAsync(
        Guid tenantId,
        Guid studentId,
        Guid? triggeringModuleId,
        CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(tenantId, ct).ConfigureAwait(false);

        BadgeEvaluationSnapshot snapshot = await LoadSnapshotAsync(tenantId, studentId, triggeringModuleId, ct)
            .ConfigureAwait(false);

        IReadOnlyList<BadgeType> newTypes = BadgeMilestoneRules.DetermineNewAwards(
            snapshot.Modules,
            snapshot.CompletedModuleIds,
            snapshot.ActivityDatesUtc,
            snapshot.ExistingBadgeTypes);
        if (newTypes.Count == 0)
            return;

        List<Badge> awarded = CreateBadgeEntities(tenantId, studentId, newTypes);
        await PersistAndNotifyAsync(tenantId, studentId, awarded, ct).ConfigureAwait(false);
    }

    private async Task<BadgeEvaluationSnapshot> LoadSnapshotAsync(
        Guid tenantId,
        Guid studentId,
        Guid? triggeringModuleId,
        CancellationToken ct)
    {
        List<BadgeMilestoneRules.ModuleRow> modules = await LoadModuleRowsAsync(tenantId, ct).ConfigureAwait(false);
        HashSet<Guid> completedIds = await LoadCompletedModuleIdsAsync(tenantId, studentId, ct).ConfigureAwait(false);
        if (triggeringModuleId is Guid moduleId)
            completedIds.Add(moduleId);

        HashSet<DateOnly> activityDates = await LoadActivityDatesAsync(tenantId, studentId, ct).ConfigureAwait(false);
        activityDates.Add(DateOnly.FromDateTime(DateTime.UtcNow));

        HashSet<BadgeType> existing = await LoadExistingBadgeTypesAsync(tenantId, studentId, ct).ConfigureAwait(false);
        return new BadgeEvaluationSnapshot(modules, completedIds, activityDates, existing);
    }

    private async Task<List<BadgeMilestoneRules.ModuleRow>> LoadModuleRowsAsync(Guid tenantId, CancellationToken ct)
    {
        return await _db.Modules
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .Select(m => new BadgeMilestoneRules.ModuleRow(m.Id, m.Subject, m.Grade))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<HashSet<Guid>> LoadCompletedModuleIdsAsync(Guid tenantId, Guid studentId, CancellationToken ct)
    {
        List<Guid> ids = await _db.StudentProgresses
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.StudentId == studentId && p.IsCompleted)
            .Select(p => p.ModuleId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return ids.ToHashSet();
    }

    private async Task<HashSet<DateOnly>> LoadActivityDatesAsync(Guid tenantId, Guid studentId, CancellationToken ct)
    {
        List<DateTime> progressDates = await _db.StudentProgresses
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.StudentId == studentId && p.CompletedAt != null)
            // CompletedAt is filtered non-null above; EF cannot promote DateTime? in this projection.
            .Select(p => p.CompletedAt!.Value)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<DateTime> attemptDates = await _db.AssessmentAttempts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.StudentId == studentId && a.SubmittedAt != null)
            // SubmittedAt is filtered non-null above; EF cannot promote DateTime? in this projection.
            .Select(a => a.SubmittedAt!.Value)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        HashSet<DateOnly> dates = new();
        foreach (DateTime at in progressDates.Concat(attemptDates))
            dates.Add(DateOnly.FromDateTime(at));
        return dates;
    }

    private async Task<HashSet<BadgeType>> LoadExistingBadgeTypesAsync(
        Guid tenantId,
        Guid studentId,
        CancellationToken ct)
    {
        List<BadgeType> types = await _db.Badges
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.StudentId == studentId)
            .Select(b => b.Type)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return types.ToHashSet();
    }

    private static List<Badge> CreateBadgeEntities(Guid tenantId, Guid studentId, IReadOnlyList<BadgeType> types)
    {
        DateTime utcNow = DateTime.UtcNow;
        return types.Select(
                type => new Badge
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StudentId = studentId,
                    Type = type,
                    EarnedAt = utcNow,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ToList();
    }

    private async Task PersistAndNotifyAsync(
        Guid tenantId,
        Guid studentId,
        List<Badge> awarded,
        CancellationToken ct)
    {
        foreach (Badge badge in awarded)
            await _db.Badges.AddAsync(badge, ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (Badge badge in awarded)
        {
            _backgroundJobs.EnqueueCertificateGeneration(tenantId, studentId, badge.Id);
            await _publisher.Publish(
                    new BadgeAwardedNotification(studentId, badge.Id, tenantId, badge.Type),
                    ct)
                .ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Awarded {BadgeCount} badge(s) to student {StudentId} in tenant {TenantId}.",
            awarded.Count,
            studentId,
            tenantId);
    }

    private sealed record BadgeEvaluationSnapshot(
        IReadOnlyList<BadgeMilestoneRules.ModuleRow> Modules,
        IReadOnlySet<Guid> CompletedModuleIds,
        IReadOnlyCollection<DateOnly> ActivityDatesUtc,
        IReadOnlySet<BadgeType> ExistingBadgeTypes);
}
