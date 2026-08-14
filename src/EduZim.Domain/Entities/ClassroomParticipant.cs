namespace EduZim.Domain.Entities;

public class ClassroomParticipant : TenantEntity
{
    public Guid ClassroomSessionId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAtUtc { get; set; }
}
