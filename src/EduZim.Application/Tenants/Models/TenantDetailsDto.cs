using EduZim.Domain.Enums;

namespace EduZim.Application.Tenants.Models;

public sealed record TenantBrandingDto(
    string SchoolName,
    string PrimaryColour,
    string? LogoUrl,
    string? SsoAuthorizationEndpoint);

public sealed record TenantDetailsDto(
    Guid Id,
    string Name,
    TenantTier Tier,
    TenantStatus Status,
    DateTime CreatedAt,
    DateTime? SuspendedAtUtc,
    TenantBrandingDto Branding);
