using EduZim.Application.Gamification.DTOs;

namespace EduZim.Application.Gamification.Services;

public static class LeaderboardBuilder
{
    public readonly record struct PointsRow(Guid StudentId, int TotalPoints, Guid TenantId, string DisplayName);

    public static LeaderboardDto Build(IReadOnlyList<PointsRow> rows, Guid tenantId, int limit)
    {
        int take = Math.Clamp(limit, 1, 100);
        List<LeaderboardEntryDto> entries = rows
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.TotalPoints)
            .ThenBy(r => r.StudentId)
            .Take(take)
            .Select((r, index) => new LeaderboardEntryDto(
                r.StudentId,
                r.TotalPoints,
                Rank: index + 1,
                r.TenantId,
                r.DisplayName))
            .ToList();

        return new LeaderboardDto(tenantId, entries);
    }
}
