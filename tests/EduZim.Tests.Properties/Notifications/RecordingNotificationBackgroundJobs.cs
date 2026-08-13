using EduZim.Application.Common.Interfaces;

namespace EduZim.Tests.Properties.Notifications;

internal sealed class RecordingNotificationBackgroundJobs : INotificationBackgroundJobs
{
    public List<(Guid TenantId, Guid NotificationId)> Scheduled { get; } = [];

    public string? ScheduleSmsRetry(Guid tenantId, Guid notificationId)
    {
        Scheduled.Add((tenantId, notificationId));
        return "sms-retry-job";
    }
}
