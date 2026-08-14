using EduZim.Domain.Enums;

namespace EduZim.Application.Gamification.DTOs;

public sealed record LeaderboardEntryDto(
    Guid StudentId,
    int TotalPoints,
    int Rank,
    Guid TenantId,
    string DisplayName);

public sealed record LeaderboardDto(Guid TenantId, IReadOnlyList<LeaderboardEntryDto> Entries);

public sealed record StudentPointsDto(Guid StudentId, int TotalPoints);

public sealed record StudentBadgeDto(Guid BadgeId, BadgeType Type, DateTime EarnedAt);

public sealed record StudentBadgesDto(Guid StudentId, IReadOnlyList<StudentBadgeDto> Badges);
