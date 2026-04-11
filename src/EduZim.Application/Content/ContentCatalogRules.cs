using EduZim.Domain.Enums;

namespace EduZim.Application.Content;

/// <summary>Invariants for catalog content (design properties 10, 11).</summary>
public static class ContentCatalogRules
{
    /// <summary>Pre-school tier video clips must not exceed 5 minutes (requirement 3.3).</summary>
    public const int PreSchoolMaxVideoDurationSeconds = 300;

    /// <returns><see langword="false"/> when tier is Pre-school, type is <see cref="ContentType.Video"/>, and duration exceeds the limit.</returns>
    public static bool IsPreSchoolVideoDurationCompliant(TenantTier tier, ContentType type, int? durationSeconds)
    {
        if (tier != TenantTier.PreSchool || type != ContentType.Video)
            return true;
        var seconds = durationSeconds ?? 0;
        return seconds <= PreSchoolMaxVideoDurationSeconds;
    }

    /// <summary>
    /// For each distinct category present, there must be at least one <see cref="ContentType.Game"/> item in that category (requirement 3.5).
    /// </summary>
    public static bool EachFoundationalConceptCategoryHasAtLeastOneGame(
        IReadOnlyCollection<(string Category, ContentType Type)> items)
    {
        if (items.Count == 0)
            return true;

        foreach (var category in items.Select(i => i.Category).Distinct())
        {
            if (!items.Any(i => i.Category == category && i.Type == ContentType.Game))
                return false;
        }

        return true;
    }
}
