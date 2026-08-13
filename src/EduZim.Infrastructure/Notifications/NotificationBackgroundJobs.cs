using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Services;
using EduZim.Infrastructure.Jobs;
using Hangfire;

namespace EduZim.Infrastructure.Notifications;

public sealed class NotificationBackgroundJobs : INotificationBackgroundJobs
{
    public string? ScheduleSmsRetry(Guid tenantId, Guid notificationId)
    {
        return BackgroundJob.Schedule<RetryFailedSmsJob>(
            job => job.RunAsync(tenantId, notificationId),
            SmsRetryRules.RetryDelay);
    }
}
