using EduZim.Domain.Enums;

namespace EduZim.Application.Content.Queries.GetModuleById;

public sealed class ModuleDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public GradeLevel Grade { get; init; }
    public string Subject { get; init; } = default!;
    public int SequenceOrder { get; init; }
    public bool IsRequired { get; init; }
    public IReadOnlyList<ModuleContentItemDetailDto> ContentItems { get; init; } = Array.Empty<ModuleContentItemDetailDto>();
}

public sealed class ModuleContentItemDetailDto
{
    public Guid ContentItemId { get; init; }
    public int SequenceOrder { get; init; }
    public string Title { get; init; } = default!;
    public ContentType Type { get; init; }
}
