using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.Services;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Marketplace.Commands.RemoveContentPack;

public sealed class RemoveContentPackCommandHandler : IRequestHandler<RemoveContentPackCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;
    private readonly ILogger<RemoveContentPackCommandHandler> _logger;

    public RemoveContentPackCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IMediator mediator,
        ILogger<RemoveContentPackCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Unit> Handle(RemoveContentPackCommand request, CancellationToken ct)
    {
        TenantAccessHelper.EnsurePlatformAdmin(_currentUser);

        ContentPack pack = await _db.ContentPacks
            .FirstOrDefaultAsync(p => p.Id == request.ContentPackId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ContentPack), request.ContentPackId);

        await _db.SetSessionTenantIdAsync(pack.TenantId, ct).ConfigureAwait(false);
        if (pack.Status == ContentPackStatus.Removed)
            return Unit.Value;

        DateTime utcNow = DateTime.UtcNow;
        pack.Status = ContentPackStatus.Removed;
        pack.RemovedAtUtc = utcNow;
        pack.UpdatedAt = utcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await NotifySubmittingTeacherAsync(pack, ct).ConfigureAwait(false);
        _logger.LogInformation("Removed marketplace content pack {PackId}.", pack.Id);
        return Unit.Value;
    }

    private async Task NotifySubmittingTeacherAsync(ContentPack pack, CancellationToken ct)
    {
        await _mediator.Send(
            new QueueNotificationCommand(
                pack.TenantId,
                pack.SubmittedByUserId,
                NotificationType.MarketplacePackRemoved,
                MarketplaceNotificationText.PackRemoved(pack.Title)),
            ct).ConfigureAwait(false);
    }
}
