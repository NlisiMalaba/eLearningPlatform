namespace EduZim.Application.Common.Auth;

public sealed record SignInWithPasswordResult
{
    public required SignInWithPasswordStatus Status { get; init; }
    public string? AccessToken { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
