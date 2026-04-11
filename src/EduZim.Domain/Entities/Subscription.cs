using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public BillingCycle Cycle { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? GracePeriodEnd { get; set; }
    public int? StudentCount { get; set; }

    /// <summary>When set to the current <see cref="CurrentPeriodEnd"/>, the renewal reminder for that period was already sent.</summary>
    public DateTime? RenewalReminderSentForPeriodEndUtc { get; set; }
}
