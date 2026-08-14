using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Marketplace.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Marketplace.Queries.GetContentPack;

public sealed class GetContentPackQueryHandler : IRequestHandler<GetContentPackQuery, ContentPackDetailDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetContentPackQueryHandler> _logger;

    public GetContentPackQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetContentPackQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ContentPackDetailDto> Handle(GetContentPackQuery request, CancellationToken ct)
    {
        MarketplaceAccess.EnsureCanBrowse(_currentUser);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        ContentPack pack = await LoadVisiblePackAsync(request, ct).ConfigureAwait(false);
        ContentPackAccessRequest? grant = await LoadGrantAsync(pack.Id, request.TenantId, ct)
            .ConfigureAwait(false);
        if (!MarketplaceAccess.CanReadPackContent(_currentUser, pack, grant))
        {
            throw new TenantAccessViolationException(
                "Access to this content pack has not been approved by the originating teacher.",
                pack.TenantId,
                pack.Id);
        }

        List<Guid> itemIds = await LoadItemIdsAsync(pack.Id, ct).ConfigureAwait(false);
        List<ContentPackRating> ratings = await LoadRatingsAsync(pack.Id, ct).ConfigureAwait(false);
        _logger.LogDebug("Returned content pack {PackId} with {ItemCount} items.", pack.Id, itemIds.Count);
        return MarketplaceMapper.ToDetailDto(pack, ratings, itemIds, hasAccess: true);
    }

    private async Task<ContentPack> LoadVisiblePackAsync(GetContentPackQuery request, CancellationToken ct)
    {
        ContentPack? pack = await _db.ContentPacks.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ContentPackId, ct)
            .ConfigureAwait(false);
        if (pack is null || !IsVisible(pack, request.TenantId, _currentUser.Role))
            throw new NotFoundException(nameof(ContentPack), request.ContentPackId);

        return pack;
    }

    private static bool IsVisible(ContentPack pack, Guid callerTenantId, UserRole role)
    {
        if (role == UserRole.PlatformAdmin)
            return pack.Status != ContentPackStatus.Removed;
        if (pack.TenantId == callerTenantId)
            return pack.Status != ContentPackStatus.Removed;
        return pack.Status == ContentPackStatus.Approved && MarketplaceMapper.HasCompleteAttribution(pack);
    }

    private async Task<ContentPackAccessRequest?> LoadGrantAsync(
        Guid packId,
        Guid requestingTenantId,
        CancellationToken ct)
    {
        return await _db.ContentPackAccessRequests.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.ContentPackId == packId && r.RequestingTenantId == requestingTenantId,
                ct)
            .ConfigureAwait(false);
    }

    private async Task<List<Guid>> LoadItemIdsAsync(Guid packId, CancellationToken ct)
    {
        return await _db.ContentPackItems.AsNoTracking()
            .Where(i => i.ContentPackId == packId)
            .OrderBy(i => i.SequenceOrder)
            .Select(i => i.ContentItemId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<List<ContentPackRating>> LoadRatingsAsync(Guid packId, CancellationToken ct)
    {
        return await _db.ContentPackRatings.AsNoTracking()
            .Where(r => r.ContentPackId == packId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
