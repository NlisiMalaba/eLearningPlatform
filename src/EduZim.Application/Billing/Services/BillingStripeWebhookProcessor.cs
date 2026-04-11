using EduZim.Application.Billing.Commands.HandlePaymentFailed;
using EduZim.Application.Billing.Commands.HandlePaymentSucceeded;
using EduZim.Application.Billing.Webhooks;
using EduZim.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Billing.Services;

public sealed class BillingStripeWebhookProcessor : IBillingStripeWebhookProcessor
{
    private readonly IMediator _mediator;
    private readonly ILogger<BillingStripeWebhookProcessor> _logger;

    public BillingStripeWebhookProcessor(IMediator mediator, ILogger<BillingStripeWebhookProcessor> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task ProcessAsync(BillingStripeNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Kind == BillingStripeNotificationKind.Ignored)
            return;

        if (notification.Kind == BillingStripeNotificationKind.PaymentSucceeded)
        {
            await _mediator.Send(
                    new HandlePaymentSucceededCommand(
                        notification.TenantId,
                        notification.SubscriptionId,
                        notification.Amount,
                        notification.Currency,
                        notification.PaymentProviderReference,
                        notification.IdempotencyKey),
                    cancellationToken)
                .ConfigureAwait(false);
            _logger.LogInformation(
                "Stripe payment succeeded processed for tenant {TenantId}, subscription {SubscriptionId}.",
                notification.TenantId,
                notification.SubscriptionId);
            return;
        }

        await _mediator.Send(
                new HandlePaymentFailedCommand(
                    notification.TenantId,
                    notification.SubscriptionId,
                    notification.FailureReason,
                    notification.PaymentProviderReference),
                cancellationToken)
            .ConfigureAwait(false);
        _logger.LogWarning(
            "Stripe payment failed processed for tenant {TenantId}, subscription {SubscriptionId}.",
            notification.TenantId,
            notification.SubscriptionId);
    }
}
