namespace EduZim.Application.Common.Interfaces;

/// <summary>Finds subscriptions expiring within the renewal window and notifies billing contacts (email + SMS).</summary>
public interface ISubscriptionRenewalReminderService
{
    Task ProcessDueRemindersAsync(CancellationToken cancellationToken);
}
