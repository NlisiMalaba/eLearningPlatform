using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.AdaptiveLearning.Services;
using EduZim.Domain.Enums;
using FsCheck;
using FsCheck.Xunit;

namespace EduZim.Tests.Properties.AdaptiveLearning;

public sealed class AdaptiveLearningPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 15: Remedial Content Recommendation Below 60% — Validates: Requirements 6.2
    [Property(MaxTest = 100)]
    public void Property15_remedial_items_when_latest_score_below_60(NonNegativeInt countGen, NonNegativeInt scoreGen)
    {
        int n = (countGen.Get % 40) + 1;
        int score = scoreGen.Get % 60;
        List<(Guid ContentItemId, string Title)> ordered = Enumerable.Range(0, n)
            .Select(i => (Guid.NewGuid(), $"item-{i}"))
            .ToList();

        IReadOnlyList<RecommendedPathItemDto> remedial = AdaptivePathRecommendationRules.BuildRemedial(ordered, score);

        Assert.NotEmpty(remedial);
        Assert.False(OracleRemedialEmpty(ordered, score));
    }

    // Feature: elearning-app-zimbabwe, Property 16: Advanced Extension Offer Above 85% — Validates: Requirements 6.3
    [Property(MaxTest = 100)]
    public void Property16_advanced_items_when_latest_score_at_or_above_85(NonNegativeInt countGen, NonNegativeInt scoreGen)
    {
        int n = (countGen.Get % 40) + 1;
        int score = 85 + (scoreGen.Get % 16);
        List<(Guid ContentItemId, string Title)> ordered = Enumerable.Range(0, n)
            .Select(i => (Guid.NewGuid(), $"item-{i}"))
            .ToList();

        IReadOnlyList<RecommendedPathItemDto> advanced = AdaptivePathRecommendationRules.BuildAdvanced(ordered, score);

        Assert.NotEmpty(advanced);
        Assert.True(advanced.Count <= 2);
        Assert.False(OracleAdvancedEmpty(ordered, score));
    }

    // Feature: elearning-app-zimbabwe, Property 17: No Grade Advancement Without Passing Score — Validates: Requirements 6.6
    [Property(MaxTest = 100)]
    public void Property17_next_grade_locked_until_required_assessments_passed(
        byte gradeSeed,
        byte moduleCountRaw)
    {
        int moduleCount = Math.Clamp(moduleCountRaw % 8 + 3, 3, 10);
        GradeLevel[] grades = Enum.GetValues<GradeLevel>();
        List<GradeAdvancementGateRules.ModuleGateRow> modules = new();
        for (int i = 0; i < moduleCount; i++)
        {
            GradeLevel g = grades[(gradeSeed + i) % grades.Length];
            modules.Add(new GradeAdvancementGateRules.ModuleGateRow(Guid.NewGuid(), g, IsRequired: i % 2 == 0));
        }

        modules.Sort((a, b) => a.Grade.CompareTo(b.Grade));

        Guid currentId = modules[modules.Count / 2].Id;

        Dictionary<Guid, List<Guid>> assessmentIdsByModule = new();
        foreach (GradeAdvancementGateRules.ModuleGateRow m in modules)
        {
            int ac = 1 + (gradeSeed % 3);
            assessmentIdsByModule[m.Id] = Enumerable.Range(0, ac).Select(_ => Guid.NewGuid()).ToList();
        }

        HashSet<Guid> passed = new();
        foreach (List<Guid> ids in assessmentIdsByModule.Values)
        {
            foreach (Guid id in ids)
            {
                if (Random.Shared.Next(2) == 0)
                    passed.Add(id);
            }
        }

        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> dict = assessmentIdsByModule.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<Guid>)kv.Value);

        (bool locked, string? reason) = GradeAdvancementGateRules.Evaluate(
            modules,
            currentId,
            dict,
            passed);

        (bool altLocked, string? altReason) = AlternateGradeGateOracle(modules, currentId, assessmentIdsByModule, passed);

        Assert.Equal(altLocked, locked);
        Assert.Equal(altReason, reason);
    }

    private static bool OracleRemedialEmpty(IReadOnlyList<(Guid ContentItemId, string Title)> ordered, int? latestScore)
    {
        if (ordered.Count == 0 || latestScore is null || latestScore >= 60)
            return true;

        return false;
    }

    private static bool OracleAdvancedEmpty(IReadOnlyList<(Guid ContentItemId, string Title)> ordered, int? latestScore)
    {
        if (ordered.Count == 0 || latestScore is null || latestScore < 85)
            return true;

        return false;
    }

    /// <summary>Independent imperative oracle for property 17.</summary>
    private static (bool Locked, string? Reason) AlternateGradeGateOracle(
        IReadOnlyList<GradeAdvancementGateRules.ModuleGateRow> modulesOrdered,
        Guid currentModuleId,
        IReadOnlyDictionary<Guid, List<Guid>> assessmentIdsByModuleId,
        IReadOnlySet<Guid> passedAssessmentIds)
    {
        bool found = false;
        GradeAdvancementGateRules.ModuleGateRow currentRow = default;
        foreach (GradeAdvancementGateRules.ModuleGateRow m in modulesOrdered)
        {
            if (m.Id == currentModuleId)
            {
                currentRow = m;
                found = true;
                break;
            }
        }

        if (!found)
            return (false, null);

        GradeLevel? nextGrade = null;
        foreach (GradeAdvancementGateRules.ModuleGateRow m in modulesOrdered)
        {
            if (m.Grade > currentRow.Grade)
            {
                nextGrade = m.Grade;
                break;
            }
        }

        if (nextGrade is null)
            return (false, null);

        foreach (GradeAdvancementGateRules.ModuleGateRow req in modulesOrdered)
        {
            if (!req.IsRequired || req.Grade >= nextGrade.Value)
                continue;

            if (!assessmentIdsByModuleId.TryGetValue(req.Id, out List<Guid>? assessments))
                continue;

            foreach (Guid aid in assessments)
            {
                if (!passedAssessmentIds.Contains(aid))
                {
                    return (
                        true,
                        $"Complete required assessments in grade with score at least 60% before grade {nextGrade.Value} modules.");
                }
            }
        }

        return (false, null);
    }
}
