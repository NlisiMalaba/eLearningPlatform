using EduZim.Domain.Enums;

namespace EduZim.Application.Content.Queries.GetContentById;

public sealed class ContentDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public ContentType Type { get; init; }
    public ContentStatus Status { get; init; }
    public string Language { get; init; } = default!;
    public long FileSizeBytes { get; init; }
    public int? DurationSeconds { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? ArchivedAtUtc { get; init; }
    public string DownloadUrl { get; init; } = default!;
}
