using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

/// <summary>A student learning session used for screen-time tracking and preschool session control.</summary>
public class StudentSession : TenantEntity
{
    public Guid StudentId { get; set; }
    public SessionStatus Status { get; set; }
    public DateOnly SessionDate { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastHeartbeatAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int AccumulatedSeconds { get; set; }
}
