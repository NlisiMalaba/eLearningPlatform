using EduZim.Application.Common.Interfaces;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Marketplace.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Marketplace.Queries.BrowseContentPacks;

public sealed class BrowseContentPacksQueryHandler
    : IRequestHandler<BrowseContentPacksQuery, IReadOnlyList<ContentPackDto>>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<BrowseContentPacksQueryHandler> _logger;

    public BrowseContentPacksQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<BrowseContentPacksQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ContentPackDto>> Handle(BrowseContentPacksQuery request, CancellationToken ct)
    {
        MarketplaceAccess.EnsureCanBrowse(_currentUser);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        List<ContentPack> packs = await LoadDiscoverablePacksAsync(ct).ConfigureAwait(false);
        List<ContentPackRating> ratings = await LoadRatingsAsync(packs, ct).ConfigureAwait(false);
        List<ContentPackAccessRequest> grants = await LoadGrantsAsync(packs, request.TenantId, ct)
            .ConfigureAwait(false);

        IReadOnlyList<ContentPackDto> result = MapPacks(packs, ratings, grants, request.TenantId);
        _logger.LogDebug("Marketplace browse returned {Count} approved packs.", result.Count);
        return result;
    }

    private async Task<List<ContentPack>> LoadDiscoverablePacksAsync(CancellationToken ct)
    {
        List<ContentPack> packs = await _db.ContentPacks.AsNoTracking()
            .Where(p => p.Status == ContentPackStatus.Approved)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return packs.Where(MarketplaceMapper.HasCompleteAttribution).ToList();
    }

    private async Task<List<ContentPackRating>> LoadRatingsAsync(
        List<ContentPack> packs,
        CancellationToken ct)
    {
        if (packs.Count == 0)
            return [];

        List<Guid> packIds = packs.Select(p => p.Id).ToList();
        return await _db.ContentPackRatings.AsNoTracking()
            .Where(r => packIds.Contains(r.ContentPackId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<List<ContentPackAccessRequest>> LoadGrantsAsync(
        List<ContentPack> packs,
        Guid callerTenantId,
        CancellationToken ct)
    {
        if (packs.Count == 0)
            return [];

        List<Guid> packIds = packs.Select(p => p.Id).ToList();
        return await _db.ContentPackAccessRequests.AsNoTracking()
            .Where(
                r => packIds.Contains(r.ContentPackId)
                    && r.RequestingTenantId == callerTenantId
                    && r.Status == ContentPackAccessStatus.Approved)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static IReadOnlyList<ContentPackDto> MapPacks(
        List<ContentPack> packs,
        List<ContentPackRating> ratings,
        List<ContentPackAccessRequest> grants,
        Guid callerTenantId)
    {
        return packs
            .Select(
                pack => MarketplaceMapper.ToDto(
                    pack,
                    ratings.Where(r => r.ContentPackId == pack.Id).ToList(),
                    MarketplaceMapper.HasApprovedAccess(callerTenantId, pack, grants)))
            .ToList();
    }
}
