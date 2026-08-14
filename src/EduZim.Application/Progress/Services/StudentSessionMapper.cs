using EduZim.Application.Progress.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Application.Progress.Services;

internal static class StudentSessionMapper
{
    public static StudentSessionDto ToDto(
        StudentSession session,
        int usedTodaySeconds,
        int? limitSeconds,
        DateTime utcNow)
    {
        bool limitReached = ScreenTimeLimitRules.IsLimitReached(limitSeconds, usedTodaySeconds);
        return new StudentSessionDto(
            session.Id,
            session.StudentId,
            session.Status,
            session.SessionDate,
            session.AccumulatedSeconds,
            usedTodaySeconds,
            limitReached,
            session.RestPromptRequired,
            session.Status == SessionStatus.Paused && !limitReached,
            PreschoolSessionRules.ContinuousInteractionSeconds(session, utcNow));
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
