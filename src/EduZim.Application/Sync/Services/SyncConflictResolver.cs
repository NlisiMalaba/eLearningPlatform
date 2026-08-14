using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Sync.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Exceptions;
using MediatR;

namespace EduZim.Application.Sync.Services;

internal static class SyncConflictResolver
{
    public static async Task<ResolveConflictResultDto> ResolveAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        ResolveConflictCommandContext context,
        CancellationToken ct)
    {
        if (context.Payload.Kind == OfflineSyncKinds.ModuleProgress)
            return await ResolveProgressAsync(db, publisher, context, ct).ConfigureAwait(false);

        if (context.Payload.Kind == OfflineSyncKinds.AssessmentAttempt)
            return await ResolveAttemptAsync(db, publisher, context, ct).ConfigureAwait(false);

        throw new DomainException("Offline sync payload kind is not supported.");
    }

    private static async Task<ResolveConflictResultDto> ResolveProgressAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        ResolveConflictCommandContext context,
        CancellationToken ct)
    {
        Guid moduleId = context.Payload.ModuleId
            ?? throw new DomainException("Module progress sync payload is missing ModuleId.");
        StudentProgress? existing = await OfflineSyncApplier
            .FindProgressAsync(db, context.TenantId, context.StudentId, moduleId, ct)
            .ConfigureAwait(false);
        if (existing is null)
            throw new NotFoundException(nameof(StudentProgress), moduleId);

        return await FinishAsync(
                db,
                publisher,
                context,
                OfflineSyncKinds.ModuleProgress,
                existing.Id,
                OfflineSyncApplier.ServerTimestamp(existing),
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<ResolveConflictResultDto> ResolveAttemptAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        ResolveConflictCommandContext context,
        CancellationToken ct)
    {
        AssessmentAttempt? existing = await OfflineSyncApplier
            .FindAttemptAsync(db, context.TenantId, context.StudentId, context.Payload, ct)
            .ConfigureAwait(false);
        if (existing is null)
            throw new NotFoundException(nameof(AssessmentAttempt), context.Payload.AssessmentId ?? Guid.Empty);

        return await FinishAsync(
                db,
                publisher,
                context,
                OfflineSyncKinds.AssessmentAttempt,
                existing.Id,
                OfflineSyncApplier.ServerTimestamp(existing),
                ct)
            .ConfigureAwait(false);
    }

    private static async Task<ResolveConflictResultDto> FinishAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        ResolveConflictCommandContext context,
        string resourceType,
        Guid resourceId,
        DateTime serverTimestamp,
        CancellationToken ct)
    {
        bool localWon = SyncConflictRules.LocalWins(context.LocalTimestamp, serverTimestamp);
        if (localWon)
        {
            await OfflineSyncApplyCoordinator
                .ApplyAndPublishAsync(
                    db, publisher, context.TenantId, context.StudentId, context.LocalTimestamp, context.Payload, ct)
                .ConfigureAwait(false);
        }

        await OfflineSyncConflictLogFactory
            .AddAsync(
                db,
                context.TenantId,
                context.StudentId,
                context.QueueItemId,
                resourceType,
                resourceId,
                context.LocalTimestamp,
                serverTimestamp,
                localWon,
                ct)
            .ConfigureAwait(false);

        return new ResolveConflictResultDto(
            localWon,
            context.LocalTimestamp,
            serverTimestamp,
            SyncConflictRules.RetainedTimestamp(context.LocalTimestamp, serverTimestamp));
    }
}

internal sealed record ResolveConflictCommandContext(
    Guid TenantId,
    Guid StudentId,
    Guid QueueItemId,
    DateTime LocalTimestamp,
    OfflineSyncPayloadDto Payload);
