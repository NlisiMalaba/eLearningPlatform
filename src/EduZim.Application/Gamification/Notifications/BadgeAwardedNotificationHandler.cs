using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Gamification.Notifications;

/// <summary>
/// Queues certificate generation and notifies linked parents (in-app and email) when a badge is awarded.
/// </summary>
public sealed class BadgeAwardedNotificationHandler : INotificationHandler<BadgeAwardedNotification>
{
    private const string FallbackStudentName = "Your student";

    private readonly IEduZimDbContext _db;
    private readonly IGamificationBackgroundJobs _backgroundJobs;
    private readonly IMediator _mediator;
    private readonly ILogger<BadgeAwardedNotificationHandler> _logger;

    public BadgeAwardedNotificationHandler(
        IEduZimDbContext db,
        IGamificationBackgroundJobs backgroundJobs,
        IMediator mediator,
        ILogger<BadgeAwardedNotificationHandler> logger)
    {
        _db = db;
        _backgroundJobs = backgroundJobs;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(BadgeAwardedNotification notification, CancellationToken ct)
    {
        _backgroundJobs.EnqueueCertificateGeneration(
            notification.TenantId,
            notification.StudentId,
            notification.BadgeId);

        await _db.SetSessionTenantIdAsync(notification.TenantId, ct).ConfigureAwait(false);

        List<Guid> parentIds = await LoadParentIdsAsync(notification, ct).ConfigureAwait(false);
        if (parentIds.Count == 0)
        {
            _logger.LogDebug(
                "Badge awarded parent notification skipped; no linked parents for student {StudentId}.",
                notification.StudentId);
            return;
        }

        string studentName = await LoadStudentDisplayNameAsync(notification, ct).ConfigureAwait(false);
        string message = BadgeAwardedParentMessage.Build(studentName, notification.BadgeType);
        await QueueParentNotificationsAsync(notification.TenantId, parentIds, message, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Queued badge certificate and parent notifications for badge {BadgeId} student {StudentId}.",
            notification.BadgeId,
            notification.StudentId);
    }

    private async Task<List<Guid>> LoadParentIdsAsync(
        BadgeAwardedNotification notification,
        CancellationToken ct)
    {
        return await _db.ParentStudentLinks
            .AsNoTracking()
            .Where(l => l.TenantId == notification.TenantId && l.StudentUserId == notification.StudentId)
            .Select(l => l.ParentUserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<string> LoadStudentDisplayNameAsync(
        BadgeAwardedNotification notification,
        CancellationToken ct)
    {
        string? name = await _db.Users
            .AsNoTracking()
            .Where(
                u => u.Id == notification.StudentId
                    && u.TenantId == notification.TenantId
                    && u.Role == UserRole.Student)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(name) ? FallbackStudentName : name.Trim();
    }

    private async Task QueueParentNotificationsAsync(
        Guid tenantId,
        List<Guid> parentIds,
        string message,
        CancellationToken ct)
    {
        foreach (Guid parentId in parentIds)
        {
            await _mediator
                .Send(
                    new QueueNotificationCommand(
                        tenantId,
                        parentId,
                        NotificationType.BadgeAwarded,
                        message),
                    ct)
                .ConfigureAwait(false);
        }
    }
}
