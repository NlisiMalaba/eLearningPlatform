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
}
