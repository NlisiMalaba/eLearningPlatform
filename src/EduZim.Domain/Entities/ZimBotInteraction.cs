namespace EduZim.Domain.Entities;

public class ZimBotInteraction : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid? ModuleId { get; set; }
    public string Question { get; set; } = default!;
    public string Response { get; set; } = default!;
    public string Language { get; set; } = "English";
    public bool UsedHintMode { get; set; }
    public bool UsedFallback { get; set; }
    public bool IsLowConfidence { get; set; }
}
