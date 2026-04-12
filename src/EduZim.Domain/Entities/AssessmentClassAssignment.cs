namespace EduZim.Domain.Entities;

public sealed class AssessmentClassAssignment : TenantEntity
{
    public Guid AssessmentId { get; set; }
    public Guid SchoolClassId { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime AssignedAtUtc { get; set; }
}
