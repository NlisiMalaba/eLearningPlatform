using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.Commands.ResolveConflict;
using EduZim.Application.Sync.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;

namespace EduZim.Application.Sync.Services;

internal static class OfflineSyncPendingProcessor
{
    public static async Task<(int Synced, int Conflicted)> ProcessAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        IMediator mediator,
        Guid tenantId,
        Guid studentId,
        IReadOnlyList<OfflineSyncQueue> pending,
        CancellationToken ct)
    {
        int synced = 0;
        int conflicted = 0;
        foreach (OfflineSyncQueue item in pending)
        {
            bool hadConflict = await ProcessOneAsync(db, publisher, mediator, tenantId, studentId, item, ct)
                .ConfigureAwait(false);
            if (hadConflict)
                conflicted++;
            else
                synced++;
        }

        return (synced, conflicted);
    }

    private static async Task<bool> ProcessOneAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        IMediator mediator,
        Guid tenantId,
        Guid studentId,
        OfflineSyncQueue item,
        CancellationToken ct)
    {
        OfflineSyncPayloadDto payload = OfflineSyncPayloadJson.Deserialize(item.Payload);
        bool conflict = await HasServerRecordAsync(db, tenantId, studentId, payload, ct).ConfigureAwait(false);
        if (conflict)
        {
            await mediator
                .Send(
                    new ResolveConflictCommand(tenantId, studentId, item.Id, item.LocalTimestamp, payload),
                    ct)
                .ConfigureAwait(false);
            item.Status = SyncStatus.Conflicted;
            return true;
        }

        await OfflineSyncApplyCoordinator
            .ApplyAndPublishAsync(db, publisher, tenantId, studentId, item.LocalTimestamp, payload, ct)
            .ConfigureAwait(false);
        item.Status = SyncStatus.Synced;
        return false;
    }

    private static async Task<bool> HasServerRecordAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        OfflineSyncPayloadDto payload,
        CancellationToken ct)
    {
        if (payload.Kind == OfflineSyncKinds.ModuleProgress)
        {
            Guid moduleId = payload.ModuleId ?? Guid.Empty;
            StudentProgress? progress = await OfflineSyncApplier
                .FindProgressAsync(db, tenantId, studentId, moduleId, ct)
                .ConfigureAwait(false);
            return progress is not null;
        }

        if (payload.Kind == OfflineSyncKinds.AssessmentAttempt)
        {
            AssessmentAttempt? attempt = await OfflineSyncApplier
                .FindAttemptAsync(db, tenantId, studentId, payload, ct)
                .ConfigureAwait(false);
            return attempt is not null;
        }

        throw new DomainException("Offline sync payload kind is not supported.");
    }
}
