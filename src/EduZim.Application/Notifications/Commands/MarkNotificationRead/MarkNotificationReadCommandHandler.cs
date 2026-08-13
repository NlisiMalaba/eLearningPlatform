using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MarkNotificationReadCommandHandler> _logger;

    public MarkNotificationReadCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<MarkNotificationReadCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        Notification notification = await LoadNotificationAsync(request, ct).ConfigureAwait(false);
        NotificationAccess.EnsureCanMarkRead(_currentUser, request.TenantId, notification.UserId);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        _logger.LogDebug("Marked notification {NotificationId} as read.", request.NotificationId);
        return Unit.Value;
    }

    private async Task<Notification> LoadNotificationAsync(MarkNotificationReadCommand request, CancellationToken ct)
    {
        Notification? notification = await _db.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == request.NotificationId && n.TenantId == request.TenantId,
                ct)
            .ConfigureAwait(false);
        if (notification is null)
            throw new NotFoundException(nameof(Notification), request.NotificationId);

        return notification;
    }
}
