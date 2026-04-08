using MediatR;

namespace EduZim.Domain.Events;

public record PaymentFailedNotification(
    Guid TenantId,
    Guid SubscriptionId,
    string? FailureReason,
    string? PaymentProviderReference)
    : INotification;
