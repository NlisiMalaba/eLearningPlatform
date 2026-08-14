using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Notifications.Commands.QueueNotification;

public sealed record QueueNotificationCommand(
    Guid TenantId,
    Guid UserId,
    NotificationType Type,
    string Message) : IRequest<Unit>;
