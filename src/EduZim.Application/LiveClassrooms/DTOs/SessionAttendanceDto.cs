namespace EduZim.Application.LiveClassrooms.DTOs;

public sealed record AttendanceEntryDto(Guid StudentUserId, DateTime JoinTimeUtc, int DurationSeconds);

public sealed record SessionAttendanceDto(Guid SessionId, IReadOnlyList<AttendanceEntryDto> Entries);
