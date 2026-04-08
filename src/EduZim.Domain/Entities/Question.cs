using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class Question
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AssessmentId { get; set; }
    public QuestionType Type { get; set; }
    public string Text { get; set; } = default!;
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public string? CorrectAnswer { get; set; }
    public int Points { get; set; }
}
