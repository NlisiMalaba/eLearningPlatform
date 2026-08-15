using System.ComponentModel.DataAnnotations;

namespace EduZim.API.Contracts;

public sealed class UpdateFontSizePreferenceRequest
{
    [Required]
    public required string FontSize { get; init; }
}
