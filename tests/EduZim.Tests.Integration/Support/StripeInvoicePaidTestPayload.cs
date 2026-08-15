using System.Globalization;
using System.Text.Json;

namespace EduZim.Tests.Integration.Support;

internal static class StripeInvoicePaidTestPayload
{
    public static string Create(string eventId, Guid tenantId, Guid subscriptionId, long amountCents, string invoiceId)
    {
        var payload = new Dictionary<string, object?>
        {
            ["id"] = eventId,
            ["object"] = "event",
            ["api_version"] = "2024-06-20",
            ["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["livemode"] = false,
            ["pending_webhooks"] = 1,
            ["type"] = "invoice.paid",
            ["request"] = new Dictionary<string, object?> { ["id"] = null, ["idempotency_key"] = null },
            ["data"] = new Dictionary<string, object?>
            {
                ["object"] = new Dictionary<string, object?>
                {
                    ["id"] = invoiceId,
                    ["object"] = "invoice",
                    ["amount_paid"] = amountCents,
                    ["amount_due"] = amountCents,
                    ["currency"] = "usd",
                    ["metadata"] = new Dictionary<string, string>
                    {
                        ["tenant_id"] = tenantId.ToString("D", CultureInfo.InvariantCulture),
                        ["subscription_id"] = subscriptionId.ToString("D", CultureInfo.InvariantCulture),
                    },
                },
            },
        };

        return JsonSerializer.Serialize(payload);
    }
}
