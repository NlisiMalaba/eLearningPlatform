using EduZim.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.ZimBot.Notifications;

public sealed class ZimBotAiUnavailableNotificationHandler : INotificationHandler<ZimBotAiUnavailableNotification>
{
    private readonly ILogger<ZimBotAiUnavailableNotificationHandler> _logger;

    public ZimBotAiUnavailableNotificationHandler(ILogger<ZimBotAiUnavailableNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ZimBotAiUnavailableNotification notification, CancellationToken ct)
    {
        _logger.LogWarning(
            "ZimBot AI unavailable for student {StudentId} in tenant {TenantId}; question length {Length}.",
            notification.StudentId,
            notification.TenantId,
            notification.Question.Length);
        return Task.CompletedTask;
    }
}
