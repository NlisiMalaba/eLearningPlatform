namespace EduZim.Domain.Entities;

/// <summary>Associates a parent/guardian user with a student within a tenant.</summary>
public class ParentStudentLink : TenantEntity
{
    public Guid ParentUserId { get; set; }
    public Guid StudentUserId { get; set; }
}
