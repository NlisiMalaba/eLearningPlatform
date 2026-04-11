using EduZim.Application.Billing.Webhooks;
using Stripe;
using Stripe.Checkout;

namespace EduZim.API.Billing;

/// <summary>Maps verified Stripe events to billing notifications (expects <c>tenant_id</c> and <c>subscription_id</c> metadata).</summary>
internal static class StripeBillingEventMapper
{
    public static BillingStripeNotification Map(Event stripeEvent)
    {
        var idempotencyKey = stripeEvent.Id;

        return stripeEvent.Type switch
        {
            EventTypes.CheckoutSessionCompleted => MapCheckoutSession(stripeEvent, idempotencyKey),
            EventTypes.InvoicePaid => MapInvoice(stripeEvent, idempotencyKey, succeeded: true),
            EventTypes.InvoicePaymentFailed => MapInvoice(stripeEvent, idempotencyKey, succeeded: false),
            _ => Ignored(),
        };
    }

    private static BillingStripeNotification Ignored() =>
        new(
            BillingStripeNotificationKind.Ignored,
            Guid.Empty,
            Guid.Empty,
            0m,
            "USD",
            null,
            null,
            null);

    private static BillingStripeNotification MapCheckoutSession(Event stripeEvent, string idempotencyKey)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null)
            return Ignored();

        if (!TryParseMetadata(session.Metadata, out var tenantId, out var subscriptionId))
            return Ignored();

        var amountTotal = session.AmountTotal ?? 0L;
        var amount = amountTotal / 100m;
        var currency = string.IsNullOrEmpty(session.Currency) ? "USD" : session.Currency.ToUpperInvariant();
        var reference = session.PaymentIntentId ?? session.Id;

        return new BillingStripeNotification(
            BillingStripeNotificationKind.PaymentSucceeded,
            tenantId,
            subscriptionId,
            amount,
            currency,
            reference,
            idempotencyKey,
            null);
    }

    private static BillingStripeNotification MapInvoice(Event stripeEvent, string idempotencyKey, bool succeeded)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice is null)
            return Ignored();

        if (!TryParseMetadata(invoice.Metadata, out var tenantId, out var subscriptionId))
            return Ignored();

        var amount = (invoice.AmountPaid > 0 ? invoice.AmountPaid : invoice.AmountDue) / 100m;
        var currency = string.IsNullOrEmpty(invoice.Currency) ? "USD" : invoice.Currency.ToUpperInvariant();
        var reference = invoice.Id;

        if (succeeded)
        {
            return new BillingStripeNotification(
                BillingStripeNotificationKind.PaymentSucceeded,
                tenantId,
                subscriptionId,
                amount,
                currency,
                reference,
                idempotencyKey,
                null);
        }

        var failure = invoice.LastFinalizationError?.Message ?? "Invoice payment failed.";
        return new BillingStripeNotification(
            BillingStripeNotificationKind.PaymentFailed,
            tenantId,
            subscriptionId,
            amount,
            currency,
            reference,
            idempotencyKey,
            failure);
    }

    private static bool TryParseMetadata(
        Dictionary<string, string>? metadata,
        out Guid tenantId,
        out Guid subscriptionId)
    {
        tenantId = Guid.Empty;
        subscriptionId = Guid.Empty;
        if (metadata is null)
            return false;
        if (!metadata.TryGetValue("tenant_id", out var t) || !Guid.TryParse(t, out tenantId))
            return false;
        if (!metadata.TryGetValue("subscription_id", out var s) || !Guid.TryParse(s, out subscriptionId))
            return false;
        return true;
    }
}
