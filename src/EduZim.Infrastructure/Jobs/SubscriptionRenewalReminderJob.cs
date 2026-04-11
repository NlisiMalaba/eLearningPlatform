using EduZim.Application.Common.Interfaces;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Daily Hangfire entry point: subscription renewal email + SMS (requirement 2.4).</summary>
public sealed class SubscriptionRenewalReminderJob
{
    private readonly ISubscriptionRenewalReminderService _renewalReminders;

    public SubscriptionRenewalReminderJob(ISubscriptionRenewalReminderService renewalReminders)
    {
        _renewalReminders = renewalReminders;
    }

    public Task RunAsync() => _renewalReminders.ProcessDueRemindersAsync(CancellationToken.None);
}
