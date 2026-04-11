using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public TenantTier Tier { get; set; }
    public TenantStatus Status { get; set; }
    public BrandingSettings Branding { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when the tenant was suspended; used for the 90-day retention window.</summary>
    public DateTime? SuspendedAtUtc { get; set; }

    /// <summary>Hangfire background job id for scheduled permanent deletion after suspension retention.</summary>
    public string? PermanentDeletionHangfireJobId { get; set; }
}
