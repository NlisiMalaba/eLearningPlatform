using System.ComponentModel.DataAnnotations;
using EduZim.Domain.Enums;

namespace EduZim.API.Contracts;

public sealed class ProvisionTenantRequest
{
    [Required]
    [MaxLength(256)]
    public required string Name { get; init; }

    public TenantTier Tier { get; init; }

    public TenantBrandingRequest? InitialBranding { get; init; }
}

public sealed class TenantBrandingRequest
{
    [MaxLength(256)]
    public string? SchoolName { get; init; }

    [MaxLength(32)]
    public string? PrimaryColour { get; init; }

    [MaxLength(1024)]
    public string? LogoUrl { get; init; }

    [MaxLength(2048)]
    public string? SsoAuthorizationEndpoint { get; init; }
}

public sealed class ProvisionTenantResponse
{
    public required Guid TenantId { get; init; }
}

public sealed class UpdateBrandingRequest
{
    [Required]
    [MaxLength(256)]
    public required string SchoolName { get; init; }

    [MaxLength(32)]
    public string PrimaryColour { get; init; } = "#1976D2";

    [MaxLength(1024)]
    public string? LogoUrl { get; init; }

    [MaxLength(2048)]
    public string? SsoAuthorizationEndpoint { get; init; }
}

public sealed class GenerateInviteCodeRequest
{
    [Required]
    public Guid StudentUserId { get; init; }
}

public sealed class GenerateInviteCodeResponse
{
    public required string Code { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}
