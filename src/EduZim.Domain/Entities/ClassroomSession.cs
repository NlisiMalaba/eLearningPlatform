namespace EduZim.Domain.Entities;

public class ClassroomSession : TenantEntity
{
    public Guid SchoolClassId { get; set; }
    public Guid TeacherUserId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime PlannedEndAtUtc { get; set; }
    public DateTime? SessionEndTime { get; set; }
    public string RoomId { get; set; } = default!;
    public string? RecordingUrl { get; set; }
}
