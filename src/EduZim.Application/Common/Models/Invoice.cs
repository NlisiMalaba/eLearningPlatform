namespace EduZim.Application.Common.Models;

public sealed class Invoice
{
    public Guid Id { get; init; }
    public Guid SubscriptionId { get; init; }
    public Guid PaymentId { get; init; }
    public DateTime IssuedAt { get; init; }
    public string? DownloadUrl { get; init; }
}
