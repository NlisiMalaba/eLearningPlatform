using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.DTOs;
using EduZim.Application.Sync.Services;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Sync.Commands.ProcessOfflineQueue;

public sealed class ProcessOfflineQueueCommandHandler
    : IRequestHandler<ProcessOfflineQueueCommand, ProcessOfflineQueueResultDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPublisher _publisher;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessOfflineQueueCommandHandler> _logger;

    public ProcessOfflineQueueCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IPublisher publisher,
        IMediator mediator,
        ILogger<ProcessOfflineQueueCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _publisher = publisher;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ProcessOfflineQueueResultDto> Handle(ProcessOfflineQueueCommand request, CancellationToken ct)
    {
        OfflineSyncAccess.EnsureCanUpload(_currentUser, request.TenantId, request.StudentId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        await OfflineSyncQueueWriter
            .EnsureStudentExistsAsync(_db, request.TenantId, request.StudentId, ct)
            .ConfigureAwait(false);

        int accepted = await OfflineSyncQueueWriter
            .EnqueueNewItemsAsync(_db, request.TenantId, request.StudentId, request.Items, ct)
            .ConfigureAwait(false);
        if (accepted > 0)
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        List<OfflineSyncQueue> pending = await OfflineSyncQueueWriter
            .LoadPendingAsync(_db, request.TenantId, request.StudentId, ct)
            .ConfigureAwait(false);
        (int synced, int conflicted) = await OfflineSyncPendingProcessor
            .ProcessAsync(_db, _publisher, _mediator, request.TenantId, request.StudentId, pending, ct)
            .ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Processed offline sync queue for student {StudentId}: accepted {Accepted}, synced {Synced}, conflicted {Conflicted}.",
            request.StudentId,
            accepted,
            synced,
            conflicted);
        return new ProcessOfflineQueueResultDto(accepted, synced, conflicted);
    }
}
