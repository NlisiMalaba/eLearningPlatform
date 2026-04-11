using MediatR;

namespace EduZim.Application.Billing.Commands.HandlePaymentFailed;

public sealed record HandlePaymentFailedCommand(
    Guid TenantId,
    Guid SubscriptionId,
    string? FailureReason,
    string? PaymentProviderReference) : IRequest;
