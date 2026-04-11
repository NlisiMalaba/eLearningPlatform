namespace EduZim.Application.Common.Configuration;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>Stripe webhook signing secret (e.g. whsec_...).</summary>
    public string WebhookSecret { get; set; } = "";
}
