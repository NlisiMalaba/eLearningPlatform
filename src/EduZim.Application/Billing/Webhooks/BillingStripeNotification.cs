namespace EduZim.Application.Billing.Webhooks;

public enum BillingStripeNotificationKind
{
    Ignored,
    PaymentSucceeded,
    PaymentFailed,
}

/// <summary>Normalized Stripe webhook payload for application billing commands (metadata-driven).</summary>
public sealed record BillingStripeNotification(
    BillingStripeNotificationKind Kind,
    Guid TenantId,
    Guid SubscriptionId,
    decimal Amount,
    string Currency,
    string? PaymentProviderReference,
    string? IdempotencyKey,
    string? FailureReason);
