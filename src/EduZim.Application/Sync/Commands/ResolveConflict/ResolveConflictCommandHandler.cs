using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.DTOs;
using EduZim.Application.Sync.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Sync.Commands.ResolveConflict;

public sealed class ResolveConflictCommandHandler : IRequestHandler<ResolveConflictCommand, ResolveConflictResultDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPublisher _publisher;
    private readonly ILogger<ResolveConflictCommandHandler> _logger;

    public ResolveConflictCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IPublisher publisher,
        ILogger<ResolveConflictCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<ResolveConflictResultDto> Handle(ResolveConflictCommand request, CancellationToken ct)
    {
        OfflineSyncAccess.EnsureCanUpload(_currentUser, request.TenantId, request.StudentId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        ResolveConflictResultDto result = await SyncConflictResolver
            .ResolveAsync(
                _db,
                _publisher,
                new ResolveConflictCommandContext(
                    request.TenantId,
                    request.StudentId,
                    request.QueueItemId,
                    request.LocalTimestamp,
                    request.Payload),
                ct)
            .ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Resolved sync conflict for queue item {QueueItemId}; localWon={LocalWon}.",
            request.QueueItemId,
            result.LocalWon);
        return result;
    }
}
