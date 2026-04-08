namespace EduZim.Domain.Entities;

public class StudentPoints : TenantEntity
{
    public Guid StudentId { get; set; }
    public int TotalPoints { get; set; }
}
