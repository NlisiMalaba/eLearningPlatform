using EduZim.Domain.Enums;

namespace EduZim.Application.Common.Models;

public sealed class CreatePaymentRequest
{
    public Guid TenantId { get; init; }
    public BillingCycle Cycle { get; init; }
    public string? CustomerEmail { get; init; }
    public string? IdempotencyKey { get; init; }
}
