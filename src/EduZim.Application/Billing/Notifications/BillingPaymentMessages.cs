namespace EduZim.Application.Billing.Notifications;

internal static class BillingPaymentMessages
{
    public const int SubscriberNotifySlaSeconds = 60;

    public static string PaymentSucceeded(string tenantName) =>
        $"Payment received. Access for {tenantName} is active.";

    public static string PaymentFailed(string tenantName, string? failureReason)
    {
        string reason = string.IsNullOrWhiteSpace(failureReason)
            ? "Payment could not be processed."
            : failureReason.Trim();
        return $"{reason} {tenantName} has a 7-day grace period before access is suspended.";
    }
}
