using System.ComponentModel.DataAnnotations;

namespace EduZim.API.Contracts;

public sealed class ZimBotChatRequest
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Message { get; set; } = default!;

    public Guid? ModuleId { get; set; }

    public bool InAssessment { get; set; }
}
