using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Progress.Commands.SendParentWeeklySummaries;

public sealed class SendParentWeeklySummariesCommandHandler
    : IRequestHandler<SendParentWeeklySummariesCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<SendParentWeeklySummariesCommandHandler> _logger;

    public SendParentWeeklySummariesCommandHandler(
        IEduZimDbContext db,
        IMediator mediator,
        ILogger<SendParentWeeklySummariesCommandHandler> logger)
    {
        _db = db;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Unit> Handle(SendParentWeeklySummariesCommand request, CancellationToken ct)
    {
        DateTime weekStart = WeeklyProgressSummaryRules.WeekStartUtc(DateTime.UtcNow);
        List<Guid> tenantIds = await LoadActiveTenantIdsAsync(ct).ConfigureAwait(false);

        foreach (Guid tenantId in tenantIds)
            await SendForTenantAsync(tenantId, weekStart, ct).ConfigureAwait(false);

        return Unit.Value;
    }

    private async Task<List<Guid>> LoadActiveTenantIdsAsync(CancellationToken ct)
    {
        return await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => t.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task SendForTenantAsync(Guid tenantId, DateTime weekStart, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(tenantId, ct).ConfigureAwait(false);

        List<ParentStudentLink> links = await _db.ParentStudentLinks
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (links.Count == 0)
            return;

        TenantWeekSnapshot snapshot = await LoadSnapshotAsync(tenantId, links, weekStart, ct)
            .ConfigureAwait(false);
        int sent = await SendToParentsAsync(tenantId, links, snapshot, weekStart, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Queued weekly progress summaries for {Count} parent(s) in tenant {TenantId}.",
            sent,
            tenantId);
    }

    private async Task<TenantWeekSnapshot> LoadSnapshotAsync(
        Guid tenantId,
        List<ParentStudentLink> links,
        DateTime weekStart,
        CancellationToken ct)
    {
        List<Guid> studentIds = links.Select(l => l.StudentUserId).Distinct().ToList();
        List<Guid> parentIds = links.Select(l => l.ParentUserId).Distinct().ToList();

        List<Module> modules = await _db.Modules.AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        List<StudentProgress> progress = await _db.StudentProgresses.AsNoTracking()
            .Where(p => p.TenantId == tenantId && studentIds.Contains(p.StudentId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        List<Badge> badges = await _db.Badges.AsNoTracking()
            .Where(b => b.TenantId == tenantId && studentIds.Contains(b.StudentId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        List<Notification> sent = await _db.Notifications.AsNoTracking()
            .Where(
                n => n.TenantId == tenantId
                    && parentIds.Contains(n.UserId)
                    && n.Type == NotificationType.WeeklyProgressSummary
                    && n.CreatedAt >= weekStart)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        Dictionary<Guid, string> labels = await LoadLabelsAsync(tenantId, studentIds, ct).ConfigureAwait(false);
        return new TenantWeekSnapshot(modules, progress, badges, sent, labels);
    }

    private async Task<Dictionary<Guid, string>> LoadLabelsAsync(
        Guid tenantId,
        List<Guid> studentIds,
        CancellationToken ct)
    {
        List<ApplicationUser> users = await _db.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && studentIds.Contains(u.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return users.ToDictionary(u => u.Id, u => u.FullName ?? string.Empty);
    }

    private async Task<int> SendToParentsAsync(
        Guid tenantId,
        List<ParentStudentLink> links,
        TenantWeekSnapshot snapshot,
        DateTime weekStart,
        CancellationToken ct)
    {
        int sent = 0;
        foreach (IGrouping<Guid, ParentStudentLink> group in links.GroupBy(l => l.ParentUserId))
        {
            List<DateTime> prior = snapshot.SentThisWeek
                .Where(n => n.UserId == group.Key)
                .Select(n => n.CreatedAt)
                .ToList();
            if (WeeklyProgressSummaryRules.AlreadySentThisWeek(prior, weekStart))
                continue;

            List<Guid> studentIds = group.Select(l => l.StudentUserId).Distinct().ToList();
            string message = WeeklySummaryComposer.Compose(
                studentIds,
                snapshot.Modules,
                snapshot.Progress,
                snapshot.Badges,
                snapshot.Labels,
                weekStart);
            await _mediator
                .Send(
                    new QueueNotificationCommand(
                        tenantId,
                        group.Key,
                        NotificationType.WeeklyProgressSummary,
                        message),
                    ct)
                .ConfigureAwait(false);
            sent++;
        }

        return sent;
    }

    private sealed record TenantWeekSnapshot(
        IReadOnlyList<Module> Modules,
        IReadOnlyList<StudentProgress> Progress,
        IReadOnlyList<Badge> Badges,
        IReadOnlyList<Notification> SentThisWeek,
        IReadOnlyDictionary<Guid, string> Labels);
}
