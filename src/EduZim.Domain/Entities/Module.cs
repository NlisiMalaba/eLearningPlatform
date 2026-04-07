using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class Module : TenantEntity
{
    public string Title { get; set; } = default!;
    public GradeLevel Grade { get; set; }
    public string Subject { get; set; } = default!;
    public int SequenceOrder { get; set; }
    public bool IsRequired { get; set; }
    public ICollection<ModuleContentItem> ContentItems { get; set; } = new List<ModuleContentItem>();
}
