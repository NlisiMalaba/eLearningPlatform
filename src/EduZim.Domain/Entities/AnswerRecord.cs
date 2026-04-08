namespace EduZim.Domain.Entities;

public class AnswerRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AssessmentAttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string? Answer { get; set; }
}
