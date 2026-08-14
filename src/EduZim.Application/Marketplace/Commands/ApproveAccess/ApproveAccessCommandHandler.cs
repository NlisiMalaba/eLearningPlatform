using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Marketplace.Commands.ApproveAccess;

public sealed class ApproveAccessCommandHandler : IRequestHandler<ApproveAccessCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ApproveAccessCommandHandler> _logger;

    public ApproveAccessCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<ApproveAccessCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(ApproveAccessCommand request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        ContentPack pack = await LoadOwnedPackAsync(request, ct).ConfigureAwait(false);
        MarketplaceAccess.EnsureCanApproveAccess(_currentUser, pack);

        ContentPackAccessRequest accessRequest = await LoadPendingRequestAsync(request, ct)
            .ConfigureAwait(false);
        DateTime utcNow = DateTime.UtcNow;
        accessRequest.Status = ContentPackAccessStatus.Approved;
        accessRequest.ReviewedAtUtc = utcNow;
        accessRequest.UpdatedAt = utcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Granted tenant {RequestingTenantId} read access to content pack {PackId} only.",
            request.RequestingTenantId,
            pack.Id);
        return Unit.Value;
    }

    private async Task<ContentPack> LoadOwnedPackAsync(ApproveAccessCommand request, CancellationToken ct)
    {
        ContentPack? pack = await _db.ContentPacks
            .FirstOrDefaultAsync(
                p => p.Id == request.ContentPackId && p.TenantId == request.TenantId,
                ct)
            .ConfigureAwait(false);
        if (pack is null)
            throw new NotFoundException(nameof(ContentPack), request.ContentPackId);

        return pack;
    }

    private async Task<ContentPackAccessRequest> LoadPendingRequestAsync(
        ApproveAccessCommand request,
        CancellationToken ct)
    {
        ContentPackAccessRequest? accessRequest = await _db.ContentPackAccessRequests
            .FirstOrDefaultAsync(
                r => r.ContentPackId == request.ContentPackId
                    && r.RequestingTenantId == request.RequestingTenantId
                    && r.TenantId == request.TenantId,
                ct)
            .ConfigureAwait(false);
        if (accessRequest is null)
            throw new NotFoundException(nameof(ContentPackAccessRequest), request.RequestingTenantId);
        if (accessRequest.Status == ContentPackAccessStatus.Approved)
            return accessRequest;
        if (accessRequest.Status != ContentPackAccessStatus.Pending)
            throw new ConflictException("This access request cannot be approved.");

        return accessRequest;
    }
}
