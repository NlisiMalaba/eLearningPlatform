using System.ComponentModel.DataAnnotations;
using EduZim.Domain.Enums;

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
    public required DateTimeOffset AccessTokenExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset RefreshTokenExpiresAt { get; init; }
}

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(8)]
    public required string Password { get; init; }

    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public UserRole Role { get; init; }
    public Guid? TenantId { get; init; }
}

public sealed class RegisterResponse
{
    public required Guid UserId { get; init; }
}

public sealed class RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}

public sealed class RevokeTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}

public sealed class LockAccountRequest
{
    /// <summary>When null, unlocks the account.</summary>
    public DateTimeOffset? LockoutEnd { get; init; }
}

public sealed class VerifyEmailBody
{
    public required Guid UserId { get; init; }
    public required string Token { get; init; }
}

public sealed class ValidateTwoFactorBody
{
    public required Guid UserId { get; init; }
    public required string Code { get; init; }
}
