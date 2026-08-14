using System.ComponentModel.DataAnnotations;

namespace EduZim.API.Contracts;

public sealed class SubmitContentPackRequest
{
    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = default!;

    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = default!;

    [Required]
    public IReadOnlyList<Guid> ContentItemIds { get; set; } = [];
}

public sealed class ApproveAccessRequest
{
    [Required]
    public Guid RequestingTenantId { get; set; }
}

public sealed class RateContentPackRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Review { get; set; }
}

public sealed class RequestAccessResponse
{
    public Guid AccessRequestId { get; init; }
}
