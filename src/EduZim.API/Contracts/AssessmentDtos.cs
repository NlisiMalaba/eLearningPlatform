using System.ComponentModel.DataAnnotations;
using EduZim.Domain.Enums;

namespace EduZim.API.Contracts;

public sealed class CreateAssessmentQuestionRequest
{
    [Required]
    public QuestionType Type { get; set; }

    [Required]
    public string Text { get; set; } = default!;

    [Range(1, int.MaxValue)]
    public int Points { get; set; }

    public IReadOnlyList<string>? OptionTexts { get; set; }

    public int? CorrectOptionIndex { get; set; }

    public string? CorrectShortAnswer { get; set; }
}

public sealed class CreateAssessmentRequest
{
    [Required]
    public Guid ModuleId { get; set; }

    [Required]
    public string Title { get; set; } = default!;

    public int? TimeLimitSeconds { get; set; }

    [Range(0, 100)]
    public int PassingScorePercent { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateAssessmentQuestionRequest> Questions { get; set; } = default!;
}

public sealed class CreateAssessmentResponse
{
    public Guid AssessmentId { get; init; }
}

public sealed class AssignAssessmentRequest
{
    [Required]
    public Guid SchoolClassId { get; set; }

    [Required]
    public DateTime DueAtUtc { get; set; }
}

public sealed class SubmitAssessmentAnswerRequest
{
    [Required]
    public Guid QuestionId { get; set; }

    public string? Answer { get; set; }
}

public sealed class SubmitAssessmentRequest
{
    [Required]
    public Guid AttemptId { get; set; }

    [Required]
    [MinLength(1)]
    public List<SubmitAssessmentAnswerRequest> Answers { get; set; } = default!;
}
