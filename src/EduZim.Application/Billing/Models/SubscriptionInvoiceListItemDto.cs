namespace EduZim.Application.Billing.Models;

public sealed record SubscriptionInvoiceListItemDto(
    Guid Id,
    Guid SubscriptionId,
    Guid PaymentId,
    DateTime IssuedAtUtc,
    string FileName);
