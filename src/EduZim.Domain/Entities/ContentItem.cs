using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class ContentItem : TenantEntity
{
    public string Title { get; set; } = default!;
    public ContentType Type { get; set; }
    public string StorageKey { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public int? DurationSeconds { get; set; }
    public string Language { get; set; } = "en";
    public ContentStatus Status { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
