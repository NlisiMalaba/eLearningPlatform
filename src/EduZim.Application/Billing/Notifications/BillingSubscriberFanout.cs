using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Billing.Notifications;

internal static class BillingSubscriberFanout
{
    public static async Task<int> NotifyAsync(
        IEduZimDbContext db,
        IMediator mediator,
        Guid tenantId,
        NotificationType type,
        string message,
        CancellationToken ct)
    {
        List<Guid> subscriberIds = await db.Users
            .AsNoTracking()
            .Where(
                u => u.TenantId == tenantId
                    && (u.Role == UserRole.SchoolAdmin || u.Role == UserRole.ParentGuardian))
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (Guid userId in subscriberIds)
        {
            await mediator
                .Send(new QueueNotificationCommand(tenantId, userId, type, message), ct)
                .ConfigureAwait(false);
        }

        return subscriberIds.Count;
    }
}
