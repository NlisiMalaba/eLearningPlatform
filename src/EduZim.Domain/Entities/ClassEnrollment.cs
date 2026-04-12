namespace EduZim.Domain.Entities;

public sealed class ClassEnrollment : TenantEntity
{
    public Guid SchoolClassId { get; set; }
    public Guid StudentUserId { get; set; }
}
