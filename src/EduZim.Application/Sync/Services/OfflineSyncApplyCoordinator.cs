using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.DTOs;
using EduZim.Domain.Events;
using EduZim.Domain.Exceptions;
using MediatR;

namespace EduZim.Application.Sync.Services;

internal static class OfflineSyncApplyCoordinator
{
    public static Task ApplyAndPublishAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        Guid tenantId,
        Guid studentId,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload,
        CancellationToken ct)
    {
        if (payload.Kind == OfflineSyncKinds.ModuleProgress)
            return ApplyModuleAsync(db, publisher, tenantId, studentId, localTimestamp, payload, ct);

        return ApplyAssessmentAsync(db, publisher, tenantId, studentId, localTimestamp, payload, ct);
    }

    private static async Task ApplyModuleAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        Guid tenantId,
        Guid studentId,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload,
        CancellationToken ct)
    {
        ApplyModuleProgressResult progress = await OfflineSyncApplier
            .ApplyModuleProgressAsync(db, tenantId, studentId, localTimestamp, payload, ct)
            .ConfigureAwait(false);
        Guid moduleId = payload.ModuleId
            ?? throw new DomainException("Module progress sync payload is missing ModuleId.");
        if (!progress.NewlyCompleted)
            return;

        await publisher
            .Publish(new ModuleCompletedNotification(studentId, moduleId, tenantId), ct)
            .ConfigureAwait(false);
    }

    private static async Task ApplyAssessmentAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        Guid tenantId,
        Guid studentId,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload,
        CancellationToken ct)
    {
        ApplyAssessmentResult attempt = await OfflineSyncApplier
            .ApplyAssessmentAsync(db, tenantId, studentId, localTimestamp, payload, ct)
            .ConfigureAwait(false);
        if (!attempt.NewlySubmitted)
            return;

        Guid assessmentId = payload.AssessmentId
            ?? throw new DomainException("Assessment attempt sync payload is missing AssessmentId.");
        await publisher
            .Publish(
                new AssessmentSubmittedNotification(
                    studentId,
                    assessmentId,
                    attempt.AttemptId,
                    tenantId,
                    attempt.ScorePercent),
                ct)
            .ConfigureAwait(false);
    }
}
