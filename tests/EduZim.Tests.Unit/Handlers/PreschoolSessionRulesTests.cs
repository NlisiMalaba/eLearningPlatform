using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Unit.Handlers;

public sealed class PreschoolSessionRulesTests
{
    [Fact]
    public void Applies_only_to_preschool_tier()
    {
        Assert.True(PreschoolSessionRules.AppliesTo(TenantTier.PreSchool));
        Assert.False(PreschoolSessionRules.AppliesTo(TenantTier.School));
        Assert.False(PreschoolSessionRules.AppliesTo(null));
    }

    [Fact]
    public void Pauses_when_idle_for_sixty_seconds()
    {
        DateTime utcNow = DateTime.UtcNow;
        StudentSession session = Session(utcNow, lastInteractionAt: utcNow.AddSeconds(-60));

        Assert.True(PreschoolSessionRules.ShouldPauseForInactivity(session, utcNow));
        Assert.Equal(60, PreschoolSessionRules.IdleSeconds(session, utcNow));
    }

    [Fact]
    public void Does_not_pause_when_idle_under_sixty_seconds()
    {
        DateTime utcNow = DateTime.UtcNow;
        StudentSession session = Session(utcNow, lastInteractionAt: utcNow.AddSeconds(-59));

        Assert.False(PreschoolSessionRules.ShouldPauseForInactivity(session, utcNow));
    }

    [Fact]
    public void Prompts_rest_after_twenty_minutes()
    {
        DateTime utcNow = DateTime.UtcNow;
        StudentSession session = Session(
            utcNow,
            lastInteractionAt: utcNow,
            segmentStartedAt: utcNow.AddSeconds(-PreschoolSessionRules.RestPromptAfterSeconds));

        Assert.True(PreschoolSessionRules.ShouldPromptRest(session, utcNow));
        Assert.Equal(
            PreschoolSessionRules.RestPromptAfterSeconds,
            PreschoolSessionRules.ContinuousInteractionSeconds(session, utcNow));
    }

    [Fact]
    public void Does_not_prompt_rest_before_twenty_minutes()
    {
        DateTime utcNow = DateTime.UtcNow;
        StudentSession session = Session(
            utcNow,
            lastInteractionAt: utcNow,
            segmentStartedAt: utcNow.AddSeconds(-(PreschoolSessionRules.RestPromptAfterSeconds - 1)));

        Assert.False(PreschoolSessionRules.ShouldPromptRest(session, utcNow));
    }

    private static StudentSession Session(
        DateTime utcNow,
        DateTime lastInteractionAt,
        DateTime? segmentStartedAt = null)
    {
        return new StudentSession
        {
            Status = SessionStatus.Active,
            StartedAt = utcNow.AddHours(-1),
            LastHeartbeatAt = utcNow,
            LastInteractionAt = lastInteractionAt,
            SegmentStartedAt = segmentStartedAt ?? utcNow.AddHours(-1),
        };
    }
}
