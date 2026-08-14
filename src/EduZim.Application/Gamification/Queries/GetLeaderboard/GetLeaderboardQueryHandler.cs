using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using EduZim.Application.Gamification.Services;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Gamification.Queries.GetLeaderboard;

public sealed class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, LeaderboardDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetLeaderboardQueryHandler> _logger;

    public GetLeaderboardQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetLeaderboardQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<LeaderboardDto> Handle(GetLeaderboardQuery request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanViewLeaderboard(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        IReadOnlyList<LeaderboardBuilder.PointsRow> rows = await LoadTenantPointsRowsAsync(request.TenantId, ct)
            .ConfigureAwait(false);
        LeaderboardDto dto = LeaderboardBuilder.Build(rows, request.TenantId, request.Limit);

        _logger.LogDebug(
            "Leaderboard for tenant {TenantId} returned {EntryCount} entries.",
            request.TenantId,
            dto.Entries.Count);

        return dto;
    }

    private async Task<IReadOnlyList<LeaderboardBuilder.PointsRow>> LoadTenantPointsRowsAsync(
        Guid tenantId,
        CancellationToken ct)
    {
        List<StudentPoints> points = await _db.StudentPoints
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (points.Count == 0)
            return Array.Empty<LeaderboardBuilder.PointsRow>();

        Dictionary<Guid, string?> names = await LoadDisplayNamesAsync(tenantId, points, ct).ConfigureAwait(false);
        return points
            .Select(
                p => new LeaderboardBuilder.PointsRow(
                    p.StudentId,
                    p.TotalPoints,
                    p.TenantId,
                    names.GetValueOrDefault(p.StudentId) ?? string.Empty))
            .ToList();
    }

    private async Task<Dictionary<Guid, string?>> LoadDisplayNamesAsync(
        Guid tenantId,
        List<StudentPoints> points,
        CancellationToken ct)
    {
        List<Guid> studentIds = points.Select(p => p.StudentId).Distinct().ToList();
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Role == UserRole.Student && studentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct)
            .ConfigureAwait(false);
    }
}
