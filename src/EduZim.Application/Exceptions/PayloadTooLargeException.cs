namespace EduZim.Application.Exceptions;

/// <summary>Thrown when a request payload exceeds configured limits (maps to HTTP 413).</summary>
public sealed class PayloadTooLargeException : Exception
{
    public PayloadTooLargeException(string message)
        : base(message)
    {
    }
}
