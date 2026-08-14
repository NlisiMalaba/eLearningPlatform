using System.ComponentModel.DataAnnotations;
using EduZim.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace EduZim.API.Contracts;

public sealed class UploadContentForm
{
    [Required]
    public string Title { get; set; } = default!;

    [Required]
    public ContentType Type { get; set; }

    public string Language { get; set; } = "en";

    [Required]
    public IFormFile File { get; set; } = default!;
}

public sealed class UploadContentResponse
{
    public Guid ContentId { get; init; }
}

public sealed class CreateModuleRequest
{
    [Required]
    public string Title { get; set; } = default!;

    [Required]
    public GradeLevel Grade { get; set; }

    [Required]
    public string Subject { get; set; } = default!;

    public int SequenceOrder { get; set; }

    public bool IsRequired { get; set; }
}

public sealed class CreateModuleResponse
{
    public Guid ModuleId { get; init; }
}

public sealed class SetModuleContentItemsRequest
{
    [Required]
    public List<Guid> ContentItemIds { get; set; } = [];
}
