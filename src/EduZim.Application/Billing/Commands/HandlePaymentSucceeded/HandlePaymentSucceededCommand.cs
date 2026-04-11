using MediatR;

namespace EduZim.Application.Billing.Commands.HandlePaymentSucceeded;

public sealed record HandlePaymentSucceededCommand(
    Guid TenantId,
    Guid SubscriptionId,
    decimal Amount,
    string Currency,
    string? PaymentProviderReference,
    string? IdempotencyKey) : IRequest;
