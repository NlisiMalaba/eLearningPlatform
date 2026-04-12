namespace EduZim.Application.AdaptiveLearning.DTOs;

public sealed record StudentLearningProfileDto(
    Guid StudentId,
    DateTime UpdatedAtUtc,
    Guid LastAssessmentId,
    Guid LastModuleId,
    int LastScorePercent,
    int DifficultyTier);

public sealed record AdjustedDifficultyDto(int DifficultyTier, int BaselineTier, int LastScorePercent);

public sealed record RecommendedPathItemDto(Guid ContentItemId, string Title, string Rationale);

public sealed record RecommendedPathDto(
    Guid ModuleId,
    int? LatestScorePercent,
    IReadOnlyList<RecommendedPathItemDto> RemedialItems,
    IReadOnlyList<RecommendedPathItemDto> AdvancedItems,
    bool NextGradeModuleLocked,
    string? NextGradeLockReason);

public sealed record WeeklyAdaptiveSummaryDto(
    int IsoYear,
    int IsoWeek,
    DateTime WeekStartUtc,
    DateTime WeekEndUtc,
    int AssessmentsSubmittedCount,
    double? AverageScorePercent,
    int TotalAssessmentTimeSeconds,
    int ModulesMarkedCompleteInWeek);
