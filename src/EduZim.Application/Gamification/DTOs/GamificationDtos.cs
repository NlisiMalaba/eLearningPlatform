namespace EduZim.Application.Gamification.DTOs;

public sealed record LeaderboardEntryDto(
    Guid StudentId,
    int TotalPoints,
    int Rank,
    Guid TenantId,
    string DisplayName);

public sealed record LeaderboardDto(Guid TenantId, IReadOnlyList<LeaderboardEntryDto> Entries);
