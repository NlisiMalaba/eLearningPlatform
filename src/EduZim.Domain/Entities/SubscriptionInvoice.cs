namespace EduZim.Domain.Entities;

/// <summary>Stored PDF invoice for a successful subscription payment.</summary>
public class SubscriptionInvoice
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid PaymentId { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public byte[] PdfContent { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "invoice.pdf";
}
