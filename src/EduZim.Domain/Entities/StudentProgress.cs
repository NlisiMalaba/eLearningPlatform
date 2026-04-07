namespace EduZim.Domain.Entities;

public class StudentProgress : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid ModuleId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TimeOnTaskSeconds { get; set; }
}
