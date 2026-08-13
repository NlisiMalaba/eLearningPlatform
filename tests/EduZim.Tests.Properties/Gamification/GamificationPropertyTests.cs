using EduZim.Application.Gamification.DTOs;
using EduZim.Application.Gamification.Services;
using EduZim.Domain.Enums;
using FsCheck;
using FsCheck.Xunit;

namespace EduZim.Tests.Properties.Gamification;

public sealed class GamificationPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 25: Points Awarded on Module and Assessment Completion — Validates: Requirements 9.3
    [Property(MaxTest = 100)]
    public void Property25_module_and_assessment_awards_meet_base_and_bonus_rules(NonNegativeInt scoreGen)
    {
        int score = scoreGen.Get % 101;
        int moduleAward = PointsAwardRules.ForModuleCompletion();
        int assessmentAward = PointsAwardRules.ForAssessment(score);

        Assert.True(moduleAward >= PointsAwardRules.ModuleCompletionBasePoints);
        Assert.True(assessmentAward >= PointsAwardRules.AssessmentCompletionBasePoints);
        if (score >= PointsAwardRules.HighScoreThresholdPercent)
        {
            Assert.Equal(
                PointsAwardRules.ApplyBonusMultiplier(PointsAwardRules.AssessmentCompletionBasePoints),
                assessmentAward);
        }
        else
        {
            Assert.Equal(PointsAwardRules.AssessmentCompletionBasePoints, assessmentAward);
        }
    }

    // Feature: elearning-app-zimbabwe, Property 26: Badge Awarded on Milestone Events — Validates: Requirements 9.4, 9.6
    [Property(MaxTest = 100)]
    public void Property26_milestone_events_produce_matching_badge_types(
        byte moduleCountRaw,
        bool firstModule,
        bool fiveDays,
        bool subjectMastery,
        bool gradeCompletion)
    {
        int moduleCount = Math.Clamp((moduleCountRaw % 6) + 1, 1, 6);
        List<BadgeMilestoneRules.ModuleRow> modules = new();
        HashSet<Guid> completed = new();
        string subject = "Math";
        GradeLevel grade = GradeLevel.Grade3;
        for (int i = 0; i < moduleCount; i++)
        {
            Guid id = Guid.NewGuid();
            modules.Add(new BadgeMilestoneRules.ModuleRow(id, subject, grade));
            if (subjectMastery || gradeCompletion || (firstModule && i == 0))
                completed.Add(id);
        }

        if (firstModule && completed.Count == 0)
            completed.Add(modules[0].Id);

        List<DateOnly> dates = new();
        DateOnly start = new(2026, 3, 1);
        int dayCount = fiveDays ? BadgeMilestoneRules.ConsecutiveDaysRequired : 2;
        for (int i = 0; i < dayCount; i++)
            dates.Add(start.AddDays(i));

        IReadOnlyList<BadgeType> awards = BadgeMilestoneRules.DetermineNewAwards(
            modules,
            completed,
            dates,
            alreadyEarned: new HashSet<BadgeType>());

        if (firstModule || subjectMastery || gradeCompletion)
            Assert.Contains(BadgeType.FirstModule, awards);
        if (fiveDays)
            Assert.Contains(BadgeType.FiveConsecutiveDays, awards);
        if ((subjectMastery || gradeCompletion) && moduleCount >= 1)
        {
            Assert.Contains(BadgeType.SubjectMastery, awards);
            Assert.Contains(BadgeType.GradeCompletion, awards);
        }

        Assert.Empty(
            BadgeMilestoneRules.DetermineNewAwards(modules, completed, dates, alreadyEarned: awards.ToHashSet()));
    }

    // Feature: elearning-app-zimbabwe, Property 27: Leaderboard Tenant Isolation — Validates: Requirements 9.5
    [Property(MaxTest = 100)]
    public void Property27_leaderboard_entries_all_belong_to_requesting_tenant(
        byte homeCountRaw,
        byte otherCountRaw,
        byte limitRaw)
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        int homeCount = homeCountRaw % 20;
        int otherCount = otherCountRaw % 20;
        int limit = (limitRaw % 100) + 1;

        List<LeaderboardBuilder.PointsRow> rows = new();
        for (int i = 0; i < homeCount; i++)
            rows.Add(new LeaderboardBuilder.PointsRow(Guid.NewGuid(), i + 1, tenantA, $"a-{i}"));
        for (int i = 0; i < otherCount; i++)
            rows.Add(new LeaderboardBuilder.PointsRow(Guid.NewGuid(), 1000 + i, tenantB, $"b-{i}"));

        LeaderboardDto dto = LeaderboardBuilder.Build(rows, tenantA, limit);

        Assert.All(dto.Entries, e => Assert.Equal(tenantA, e.TenantId));
        Assert.DoesNotContain(dto.Entries, e => e.TenantId == tenantB);
        Assert.True(dto.Entries.Count <= Math.Min(limit, homeCount));
        Assert.Equal(tenantA, dto.TenantId);
    }
}
