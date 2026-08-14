using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Domain.Entities;

namespace EduZim.Application.LiveClassrooms.Services;

public static class AttendanceMapper
{
    public static SessionAttendanceDto ToDto(Guid sessionId, IReadOnlyList<AttendanceRecord> rows)
    {
        IReadOnlyList<AttendanceEntryDto> entries = rows
            .OrderBy(r => r.JoinTimeUtc)
            .Select(r => new AttendanceEntryDto(r.StudentUserId, r.JoinTimeUtc, r.DurationSeconds))
            .ToList();
        return new SessionAttendanceDto(sessionId, entries);
    }
}
