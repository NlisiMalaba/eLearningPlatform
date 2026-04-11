using EduZim.Domain.Enums;

namespace EduZim.Application.Common.Models;

public sealed class InvoicePdfModel
{
    public Guid InvoiceId { get; init; }
    public Guid SubscriptionId { get; init; }
    public Guid TenantId { get; init; }
    public required string TenantName { get; init; }
    public DateTime IssuedAtUtc { get; init; }
    public BillingCycle Cycle { get; init; }
    public int StudentCount { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? PaymentProviderReference { get; init; }
}
