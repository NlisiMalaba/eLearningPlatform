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
}
