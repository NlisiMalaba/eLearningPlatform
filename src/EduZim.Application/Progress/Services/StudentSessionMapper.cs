using EduZim.Application.Progress.DTOs;
using EduZim.Domain.Entities;

namespace EduZim.Application.Progress.Services;

internal static class StudentSessionMapper
{
    public static StudentSessionDto ToDto(
        StudentSession session,
        int usedTodaySeconds,
        int? limitSeconds)
    {
        return new StudentSessionDto(
            session.Id,
            session.StudentId,
            session.Status,
            session.SessionDate,
            session.AccumulatedSeconds,
            usedTodaySeconds,
            ScreenTimeLimitRules.IsLimitReached(limitSeconds, usedTodaySeconds));
    }

    public static ScreenTimeLimitRules.SessionSlice ToSlice(StudentSession session)
    {
        return new ScreenTimeLimitRules.SessionSlice(
            session.SessionDate,
            session.Status,
            session.AccumulatedSeconds,
            session.LastHeartbeatAt);
    }
}
