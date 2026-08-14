using EduZim.Domain.Enums;

namespace EduZim.Application.Progress.Services;

/// <summary>Daily screen-time limit enforcement (correctness property 31).</summary>
public static class ScreenTimeLimitRules
{
    public const int MinLimitSeconds = 1;
    public const int MaxLimitSeconds = 86_400;
    private const int ZimbabweUtcOffsetHours = 2;

    public readonly record struct SessionSlice(
        DateOnly SessionDate,
        SessionStatus Status,
        int AccumulatedSeconds,
        DateTime LastHeartbeatAt);

    public readonly record struct TickResult(int AccumulatedSeconds, bool Pause);

    public static DateOnly CalendarDay(DateTime utcNow)
    {
        DateTime local = utcNow.AddHours(ZimbabweUtcOffsetHours);
        return DateOnly.FromDateTime(local);
    }

    public static int ElapsedSeconds(DateTime lastHeartbeatAt, DateTime utcNow)
    {
        if (utcNow <= lastHeartbeatAt)
            return 0;

        return (int)Math.Floor((utcNow - lastHeartbeatAt).TotalSeconds);
    }

    public static bool HasLimit(int? limitSeconds) => limitSeconds is > 0;

    public static bool IsLimitReached(int? limitSeconds, int usedSeconds) =>
        HasLimit(limitSeconds) && usedSeconds >= limitSeconds.GetValueOrDefault();

    public static bool CanCreateSession(int? limitSeconds, int usedTodaySeconds) =>
        !IsLimitReached(limitSeconds, usedTodaySeconds);

    public static int UsedTodaySeconds(
        IReadOnlyList<SessionSlice> sessions,
        DateOnly today,
        DateTime utcNow)
    {
        int used = 0;
        foreach (SessionSlice session in sessions)
        {
            if (session.SessionDate != today)
                continue;

            used += session.AccumulatedSeconds;
            if (session.Status == SessionStatus.Active)
                used += ElapsedSeconds(session.LastHeartbeatAt, utcNow);
        }

        return used;
    }

    public static TickResult Tick(
        int accumulatedSeconds,
        DateTime lastHeartbeatAt,
        DateTime utcNow,
        int usedByOtherSessionsToday,
        int? limitSeconds)
    {
        int elapsed = ElapsedSeconds(lastHeartbeatAt, utcNow);
        int uncapped = accumulatedSeconds + elapsed;
        int next = CapToLimit(uncapped, usedByOtherSessionsToday, limitSeconds);
        int usedAfter = usedByOtherSessionsToday + next;
        return new TickResult(next, IsLimitReached(limitSeconds, usedAfter));
    }

    private static int CapToLimit(int uncapped, int usedByOtherSessionsToday, int? limitSeconds)
    {
        if (!HasLimit(limitSeconds))
            return uncapped;

        int remaining = Math.Max(0, limitSeconds.GetValueOrDefault() - usedByOtherSessionsToday);
        return Math.Min(uncapped, remaining);
    }
}
