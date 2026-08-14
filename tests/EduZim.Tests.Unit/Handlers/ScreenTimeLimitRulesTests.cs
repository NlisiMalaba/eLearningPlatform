using EduZim.Application.Progress.Services;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ScreenTimeLimitRulesTests
{
    [Fact]
    public void Tick_pauses_and_caps_when_limit_is_reached()
    {
        DateTime heartbeat = DateTime.UtcNow.AddSeconds(-30);
        ScreenTimeLimitRules.TickResult result = ScreenTimeLimitRules.Tick(
            accumulatedSeconds: 80,
            lastHeartbeatAt: heartbeat,
            utcNow: heartbeat.AddSeconds(30),
            usedByOtherSessionsToday: 0,
            limitSeconds: 100);

        Assert.Equal(100, result.AccumulatedSeconds);
        Assert.True(result.Pause);
    }

    [Fact]
    public void Tick_does_not_pause_when_under_limit()
    {
        DateTime heartbeat = DateTime.UtcNow;
        ScreenTimeLimitRules.TickResult result = ScreenTimeLimitRules.Tick(
            accumulatedSeconds: 10,
            lastHeartbeatAt: heartbeat,
            utcNow: heartbeat.AddSeconds(5),
            usedByOtherSessionsToday: 0,
            limitSeconds: 100);

        Assert.Equal(15, result.AccumulatedSeconds);
        Assert.False(result.Pause);
    }

    [Fact]
    public void CanCreateSession_is_false_when_used_meets_limit()
    {
        Assert.False(ScreenTimeLimitRules.CanCreateSession(100, 100));
        Assert.False(ScreenTimeLimitRules.CanCreateSession(100, 150));
        Assert.True(ScreenTimeLimitRules.CanCreateSession(100, 99));
        Assert.True(ScreenTimeLimitRules.CanCreateSession(null, 10_000));
    }

    [Fact]
    public void UsedToday_includes_active_elapsed_only_for_today()
    {
        DateOnly today = ScreenTimeLimitRules.CalendarDay(DateTime.UtcNow);
        DateTime now = DateTime.UtcNow;
        List<ScreenTimeLimitRules.SessionSlice> slices =
        [
            new(today, SessionStatus.Paused, 40, now),
            new(today, SessionStatus.Active, 10, now.AddSeconds(-5)),
            new(today.AddDays(-1), SessionStatus.Paused, 999, now),
        ];

        int used = ScreenTimeLimitRules.UsedTodaySeconds(slices, today, now);
        Assert.Equal(55, used);
    }

    [Fact]
    public void Calendar_day_uses_zimbabwe_offset()
    {
        DateTime utc = new(2026, 8, 14, 22, 30, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 8, 15), ScreenTimeLimitRules.CalendarDay(utc));
    }
}
