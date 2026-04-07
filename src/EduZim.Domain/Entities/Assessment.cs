namespace EduZim.Domain.Entities;

public class Assessment : TenantEntity
{
    public string Title { get; set; } = default!;
    public Guid ModuleId { get; set; }
    public int? TimeLimitSeconds { get; set; }
    public int PassingScorePercent { get; set; } = 60;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
