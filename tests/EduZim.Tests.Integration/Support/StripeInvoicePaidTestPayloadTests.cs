using Stripe;

namespace EduZim.Tests.Integration.Support;

public sealed class StripeInvoicePaidTestPayloadTests
{
    [Fact]
    public void Signed_invoice_paid_json_deserializes_with_metadata()
    {
        Guid tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid subscriptionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        const string secret = "whsec_payload_check";
        string json = StripeInvoicePaidTestPayload.Create(
            "evt_payload_check",
            tenantId,
            subscriptionId,
            2200,
            "in_payload_check");
        string header = StripeWebhookTestSignature.CreateHeader(json, secret);

        Event stripeEvent = EventUtility.ConstructEvent(
            json,
            header,
            secret,
            throwOnApiVersionMismatch: false);

        Assert.Equal("invoice.paid", stripeEvent.Type);
        Assert.Equal("evt_payload_check", stripeEvent.Id);
        Invoice invoice = Assert.IsType<Invoice>(stripeEvent.Data.Object);
        Assert.Equal(tenantId.ToString(), invoice.Metadata["tenant_id"]);
        Assert.Equal(subscriptionId.ToString(), invoice.Metadata["subscription_id"]);
        Assert.Equal(2200, invoice.AmountPaid);
    }
}
