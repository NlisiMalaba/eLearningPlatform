using EduZim.Application.Billing.Webhooks;

namespace EduZim.Application.Common.Interfaces;

public interface IBillingStripeWebhookProcessor
{
    Task ProcessAsync(BillingStripeNotification notification, CancellationToken cancellationToken);
}
