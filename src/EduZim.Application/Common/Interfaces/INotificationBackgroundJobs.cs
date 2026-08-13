namespace EduZim.Application.Common.Interfaces;

/// <summary>Schedules Hangfire retries for failed SMS notifications (requirement 15.5).</summary>
public interface INotificationBackgroundJobs
{
    string? ScheduleSmsRetry(Guid tenantId, Guid notificationId);
}
