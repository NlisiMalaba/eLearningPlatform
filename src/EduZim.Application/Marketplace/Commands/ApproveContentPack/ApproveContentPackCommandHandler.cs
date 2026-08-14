using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Marketplace.Services;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Marketplace.Commands.ApproveContentPack;

public sealed class ApproveContentPackCommandHandler : IRequestHandler<ApproveContentPackCommand, ContentPackDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ApproveContentPackCommandHandler> _logger;

    public ApproveContentPackCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<ApproveContentPackCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ContentPackDto> Handle(ApproveContentPackCommand request, CancellationToken ct)
    {
        TenantAccessHelper.EnsurePlatformAdmin(_currentUser);

        ContentPack pack = await _db.ContentPacks
            .FirstOrDefaultAsync(p => p.Id == request.ContentPackId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ContentPack), request.ContentPackId);

        await _db.SetSessionTenantIdAsync(pack.TenantId, ct).ConfigureAwait(false);
        if (pack.Status == ContentPackStatus.Approved)
            return MarketplaceMapper.ToDto(pack, [], hasAccess: true);
        if (pack.Status != ContentPackStatus.PendingReview)
            throw new ConflictException("Only packs pending review can be approved.");

        pack.Status = ContentPackStatus.Approved;
        pack.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Approved content pack {PackId} for marketplace discovery.", pack.Id);
        return MarketplaceMapper.ToDto(pack, [], hasAccess: true);
    }
}
