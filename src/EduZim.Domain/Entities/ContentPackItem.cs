namespace EduZim.Domain.Entities;

public class ContentPackItem : TenantEntity
{
    public Guid ContentPackId { get; set; }
    public Guid ContentItemId { get; set; }
    public int SequenceOrder { get; set; }
}
