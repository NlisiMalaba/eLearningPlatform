using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Marketplace.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Marketplace.Commands.RateContentPack;

public sealed class RateContentPackCommandHandler : IRequestHandler<RateContentPackCommand, ContentPackDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RateContentPackCommandHandler> _logger;

    public RateContentPackCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<RateContentPackCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ContentPackDto> Handle(RateContentPackCommand request, CancellationToken ct)
    {
        MarketplaceAccess.EnsureCanRate(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        ContentPack pack = await LoadApprovedPackAsync(request.ContentPackId, ct).ConfigureAwait(false);
        if (pack.TenantId == request.TenantId)
            throw new ConflictException("You cannot rate a content pack from your own school.");

        await UpsertRatingAsync(pack, request, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        List<ContentPackRating> ratings = await LoadPackRatingsAsync(pack.Id, ct).ConfigureAwait(false);
        _logger.LogInformation("Recorded marketplace rating for content pack {PackId}.", pack.Id);
        return MarketplaceMapper.ToDto(pack, ratings, hasAccess: false);
    }

    private async Task<ContentPack> LoadApprovedPackAsync(Guid packId, CancellationToken ct)
    {
        ContentPack? pack = await _db.ContentPacks.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == packId && p.Status == ContentPackStatus.Approved, ct)
            .ConfigureAwait(false);
        if (pack is null)
            throw new NotFoundException(nameof(ContentPack), packId);

        return pack;
    }

    private async Task UpsertRatingAsync(ContentPack pack, RateContentPackCommand request, CancellationToken ct)
    {
        DateTime utcNow = DateTime.UtcNow;
        ContentPackRating? existing = await _db.ContentPackRatings
            .FirstOrDefaultAsync(
                r => r.ContentPackId == pack.Id && r.UserId == _currentUser.UserId,
                ct)
            .ConfigureAwait(false);
        if (existing is null)
        {
            ContentPackRating created = MarketplacePackFactory.CreateRating(
                pack,
                request.TenantId,
                _currentUser.UserId,
                request.Rating,
                request.Review,
                utcNow);
            await _db.ContentPackRatings.AddAsync(created, ct).ConfigureAwait(false);
            return;
        }

        existing.Rating = request.Rating;
        existing.Review = string.IsNullOrWhiteSpace(request.Review) ? null : request.Review.Trim();
        existing.UpdatedAt = utcNow;
    }

    private async Task<List<ContentPackRating>> LoadPackRatingsAsync(Guid packId, CancellationToken ct)
    {
        return await _db.ContentPackRatings.AsNoTracking()
            .Where(r => r.ContentPackId == packId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
