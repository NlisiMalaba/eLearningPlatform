using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Notifications.Services;

internal static class InactivityAlertFanout
{
    public static async Task NotifyLinkedParentsAsync(
        IEduZimDbContext db,
        IMediator mediator,
        ILogger logger,
        StudentInactiveNotification notification,
        CancellationToken ct)
    {
        await db.SetSessionTenantIdAsync(notification.TenantId, ct).ConfigureAwait(false);

        ApplicationUser? student = await LoadStudentAsync(db, notification, ct).ConfigureAwait(false);
        if (student is null)
            return;
        if (!InactivityAlertRules.NeedsAlert(student.LastLoginAt, student.LastInactivityAlertAt, DateTime.UtcNow))
            return;

        List<Guid> parentIds = await LoadParentIdsAsync(db, notification, ct).ConfigureAwait(false);
        if (parentIds.Count == 0)
        {
            logger.LogDebug(
                "Inactivity alert skipped; no linked parents for student {StudentId}.",
                notification.StudentId);
            return;
        }

        await QueueParentAlertsAsync(mediator, notification, parentIds, ct).ConfigureAwait(false);
        student.LastInactivityAlertAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Queued inactivity alerts for {ParentCount} parent(s) of student {StudentId}.",
            parentIds.Count,
            notification.StudentId);
    }

    private static async Task<ApplicationUser?> LoadStudentAsync(
        IEduZimDbContext db,
        StudentInactiveNotification notification,
        CancellationToken ct)
    {
        return await db.Users
            .FirstOrDefaultAsync(
                u => u.Id == notification.StudentId
                    && u.TenantId == notification.TenantId
                    && u.Role == UserRole.Student,
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<List<Guid>> LoadParentIdsAsync(
        IEduZimDbContext db,
        StudentInactiveNotification notification,
        CancellationToken ct)
    {
        return await db.ParentStudentLinks
            .AsNoTracking()
            .Where(
                l => l.TenantId == notification.TenantId && l.StudentUserId == notification.StudentId)
            .Select(l => l.ParentUserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static async Task QueueParentAlertsAsync(
        IMediator mediator,
        StudentInactiveNotification notification,
        List<Guid> parentIds,
        CancellationToken ct)
    {
        string message = InactivityAlertRules.ParentMessage();
        foreach (Guid parentId in parentIds)
        {
            await mediator
                .Send(
                    new QueueNotificationCommand(
                        notification.TenantId,
                        parentId,
                        NotificationType.InactivityAlert,
                        message),
                    ct)
                .ConfigureAwait(false);
        }
    }
}
