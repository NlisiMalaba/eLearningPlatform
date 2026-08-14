using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Application.Progress.Services;

/// <summary>Pre-school rest-prompt and inactivity pause (requirements 3.6 and 3.7).</summary>
public static class PreschoolSessionRules
{
    public const int RestPromptAfterSeconds = 20 * 60;
    public const int InactivityPauseAfterSeconds = 60;

    public static bool AppliesTo(TenantTier? tier) => tier == TenantTier.PreSchool;

    public static DateTime EffectiveLastInteractionAt(StudentSession session) =>
        session.LastInteractionAt ?? session.StartedAt;

    public static DateTime EffectiveSegmentStartedAt(StudentSession session) =>
        session.SegmentStartedAt ?? session.StartedAt;

    public static int ContinuousInteractionSeconds(StudentSession session, DateTime utcNow)
    {
        DateTime start = EffectiveSegmentStartedAt(session);
        if (utcNow <= start)
            return 0;

        return (int)Math.Floor((utcNow - start).TotalSeconds);
    }

    public static int IdleSeconds(StudentSession session, DateTime utcNow)
    {
        DateTime last = EffectiveLastInteractionAt(session);
        if (utcNow <= last)
            return 0;

        return (int)Math.Floor((utcNow - last).TotalSeconds);
    }

    public static bool ShouldPromptRest(StudentSession session, DateTime utcNow) =>
        ContinuousInteractionSeconds(session, utcNow) >= RestPromptAfterSeconds;

    public static bool ShouldPauseForInactivity(StudentSession session, DateTime utcNow) =>
        session.Status == SessionStatus.Active
        && IdleSeconds(session, utcNow) >= InactivityPauseAfterSeconds;
}
