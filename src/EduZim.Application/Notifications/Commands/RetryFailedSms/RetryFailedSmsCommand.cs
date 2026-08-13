using MediatR;

namespace EduZim.Application.Notifications.Commands.RetryFailedSms;

public sealed record RetryFailedSmsCommand(Guid TenantId, Guid NotificationId) : IRequest<Unit>;
