using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.Services;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Marketplace.Commands.RequestAccess;

public sealed class RequestAccessCommandHandler : IRequestHandler<RequestAccessCommand, Guid>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;
    private readonly ILogger<RequestAccessCommandHandler> _logger;

    public RequestAccessCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IMediator mediator,
        ILogger<RequestAccessCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Guid> Handle(RequestAccessCommand request, CancellationToken ct)
    {
        MarketplaceAccess.EnsureCanRequestAccess(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        ContentPack pack = await LoadApprovedPackAsync(request.ContentPackId, ct).ConfigureAwait(false);
        EnsureNotOwnPack(pack, request.TenantId);
        await EnsureNoExistingRequestAsync(pack.Id, request.TenantId, ct).ConfigureAwait(false);

        ContentPackAccessRequest accessRequest = MarketplacePackFactory.CreatePendingAccessRequest(
            pack,
            request.TenantId,
            _currentUser.UserId,
            DateTime.UtcNow);
        await _db.ContentPackAccessRequests.AddAsync(accessRequest, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await NotifyOriginatingTeacherAsync(pack, ct).ConfigureAwait(false);
        _logger.LogInformation(
            "Requested access to content pack {PackId} from tenant {RequestingTenantId}.",
            pack.Id,
            request.TenantId);
        return accessRequest.Id;
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

    private static void EnsureNotOwnPack(ContentPack pack, Guid requestingTenantId)
    {
        if (pack.TenantId == requestingTenantId)
            throw new ConflictException("You already have access to content packs from your own school.");
    }

    private async Task EnsureNoExistingRequestAsync(Guid packId, Guid requestingTenantId, CancellationToken ct)
    {
        ContentPackAccessRequest? existing = await _db.ContentPackAccessRequests
            .FirstOrDefaultAsync(
                r => r.ContentPackId == packId && r.RequestingTenantId == requestingTenantId,
                ct)
            .ConfigureAwait(false);
        if (existing is null)
            return;
        if (existing.Status == ContentPackAccessStatus.Approved)
            throw new ConflictException("Access to this content pack has already been granted.");
        if (existing.Status == ContentPackAccessStatus.Pending)
            throw new ConflictException("An access request for this content pack is already pending.");
    }

    private async Task NotifyOriginatingTeacherAsync(ContentPack pack, CancellationToken ct)
    {
        await _mediator.Send(
            new QueueNotificationCommand(
                pack.TenantId,
                pack.SubmittedByUserId,
                NotificationType.MarketplaceAccessRequested,
                MarketplaceNotificationText.AccessRequested(pack.Title)),
            ct).ConfigureAwait(false);
    }
}
