using Stripe;

namespace EduZim.Tests.Integration.Support;

internal static class StripeWebhookTestSignature
{
    public static string CreateHeader(string json, string secret)
    {
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        string signature = EventUtility.ComputeSignature(secret, timestamp, json);
        return $"t={timestamp},v1={signature}";
    }
}
