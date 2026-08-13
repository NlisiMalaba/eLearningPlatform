using System.ComponentModel.DataAnnotations;

namespace EduZim.API.Contracts;

public sealed class ScheduleClassroomRequest
{
    [Required]
    public Guid SchoolClassId { get; set; }

    [Required]
    public DateTime StartAtUtc { get; set; }

    [Range(1, 480)]
    public int DurationMinutes { get; set; }
}

public sealed class ScheduleClassroomResponse
{
    public Guid SessionId { get; init; }
}

public sealed class ClassroomRecordingResponse
{
    public string Url { get; init; } = default!;
}
