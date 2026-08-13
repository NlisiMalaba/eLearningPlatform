using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid TenantId, Guid NotificationId)
    : IRequest<Unit>, ITenantScopedRequest;
