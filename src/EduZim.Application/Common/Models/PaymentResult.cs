namespace EduZim.Application.Common.Models;

public sealed class PaymentResult
{
    public bool Success { get; init; }
    public string? PaymentProviderReference { get; init; }
    public string? ErrorMessage { get; init; }
}
