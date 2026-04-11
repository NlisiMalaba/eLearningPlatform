namespace EduZim.Domain.Entities;

public class CaptionTrack : TenantEntity
{
    public Guid ContentItemId { get; set; }
    public string Language { get; set; } = "en";
    public string StorageKey { get; set; } = default!;
}
