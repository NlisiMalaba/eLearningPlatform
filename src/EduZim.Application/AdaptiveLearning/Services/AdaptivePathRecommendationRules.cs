using EduZim.Application.AdaptiveLearning.DTOs;

namespace EduZim.Application.AdaptiveLearning.Services;

/// <summary>Pure rules for remedial and advanced path segments (correctness properties 15–16).</summary>
public static class AdaptivePathRecommendationRules
{
    public static IReadOnlyList<RecommendedPathItemDto> BuildRemedial(
        IReadOnlyList<(Guid ContentItemId, string Title)> orderedItems,
        int? latestScorePercent)
    {
        if (orderedItems.Count == 0 || latestScorePercent is null || latestScorePercent >= 60)
            return Array.Empty<RecommendedPathItemDto>();

        int take = Math.Max(1, orderedItems.Count / 2);
        return orderedItems
            .Take(take)
            .Select(
                x => new RecommendedPathItemDto(
                    x.ContentItemId,
                    x.Title,
                    "Remedial reinforcement (score under 60%)."))
            .ToList();
    }

    public static IReadOnlyList<RecommendedPathItemDto> BuildAdvanced(
        IReadOnlyList<(Guid ContentItemId, string Title)> orderedItems,
        int? latestScorePercent)
    {
        if (orderedItems.Count == 0 || latestScorePercent is null || latestScorePercent < 85)
            return Array.Empty<RecommendedPathItemDto>();

        int take = Math.Min(2, orderedItems.Count);
        return orderedItems
            .Skip(Math.Max(0, orderedItems.Count - take))
            .Select(
                x => new RecommendedPathItemDto(
                    x.ContentItemId,
                    x.Title,
                    "Advanced extension (score at or above 85%)."))
            .ToList();
    }
}
