using System.ComponentModel.DataAnnotations;
using EduZim.Domain.Enums;

namespace EduZim.API.Contracts;

public sealed class CreateSubscriptionRequest
{
    /// <summary>Required when the caller is a platform administrator.</summary>
    public Guid? TenantId { get; init; }

    [Required]
    public BillingCycle Cycle { get; init; }

    [Range(1, int.MaxValue)]
    public int StudentCount { get; init; }
}

public sealed class CreateSubscriptionResponse
{
    public required Guid SubscriptionId { get; init; }
}

public sealed class SubscriptionInvoiceItemResponse
{
    public required Guid Id { get; init; }
    public required Guid SubscriptionId { get; init; }
    public required Guid PaymentId { get; init; }
    public required DateTime IssuedAtUtc { get; init; }
    public required string FileName { get; init; }
}
