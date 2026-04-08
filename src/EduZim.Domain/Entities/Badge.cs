using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class Badge : TenantEntity
{
    public Guid StudentId { get; set; }
    public BadgeType Type { get; set; }
    public DateTime EarnedAt { get; set; }
}
