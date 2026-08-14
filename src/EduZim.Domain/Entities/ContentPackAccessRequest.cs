using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class ContentPackAccessRequest : TenantEntity
{
    public Guid ContentPackId { get; set; }
    public Guid RequestingTenantId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public ContentPackAccessStatus Status { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
