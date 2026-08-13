namespace EduZim.Domain.Entities;

public class AttendanceRecord : TenantEntity
{
    public Guid ClassroomSessionId { get; set; }
    public Guid StudentUserId { get; set; }
    public DateTime JoinTimeUtc { get; set; }
    public int DurationSeconds { get; set; }
}
