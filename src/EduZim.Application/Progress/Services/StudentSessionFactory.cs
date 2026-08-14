using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Application.Progress.Services;

internal static class StudentSessionFactory
{
    public static StudentSession Create(Guid tenantId, Guid studentId, DateTime utcNow)
    {
        return new StudentSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            Status = SessionStatus.Active,
            SessionDate = ScreenTimeLimitRules.CalendarDay(utcNow),
            StartedAt = utcNow,
            LastHeartbeatAt = utcNow,
            AccumulatedSeconds = 0,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }
}
