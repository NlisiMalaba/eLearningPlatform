namespace EduZim.Application.Billing.Models;

public sealed record SubscriptionInvoiceFileResult(byte[] Content, string FileName, string ContentType);
