using MediatR;

namespace EduZim.Domain.Events;

public record PaymentSucceededNotification(
    Guid TenantId,
    Guid SubscriptionId,
    string? PaymentProviderReference,
    string? IdempotencyKey)
    : INotification;
