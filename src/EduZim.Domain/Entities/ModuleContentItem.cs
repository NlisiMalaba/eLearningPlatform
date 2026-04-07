namespace EduZim.Domain.Entities;

/// <summary>
/// Join of <see cref="Module"/> and <see cref="ContentItem"/> with ordering (school-tier; RLS via <see cref="TenantEntity.TenantId"/>).
/// </summary>
public class ModuleContentItem : TenantEntity
{
    public Guid ModuleId { get; set; }
    public Guid ContentItemId { get; set; }
    public int SequenceOrder { get; set; }
}
