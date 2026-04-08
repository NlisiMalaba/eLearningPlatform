using System.ComponentModel.DataAnnotations;

namespace EduZim.API.Contracts;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}

public sealed class LoginResponse
{
    public required string AccessToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
