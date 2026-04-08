namespace EduZim.Domain.Entities;

public class AssessmentAttempt : TenantEntity
{
    public Guid AssessmentId { get; set; }
    public Guid StudentId { get; set; }
    public int ScorePercent { get; set; }
    public int TimeTakenSeconds { get; set; }
    public DateTime SubmittedAt { get; set; }
    public ICollection<AnswerRecord> Answers { get; set; } = new List<AnswerRecord>();
}
