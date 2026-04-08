namespace EduZim.Application.Common.Models;

public sealed class SmsResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
