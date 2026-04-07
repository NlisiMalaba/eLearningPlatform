namespace EduZim.Domain.Entities;

public class QuestionOption
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = default!;
    public int OrderIndex { get; set; }
}
