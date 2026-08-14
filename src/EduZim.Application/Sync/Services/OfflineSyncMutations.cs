using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.DTOs;
using EduZim.Domain.Entities;

namespace EduZim.Application.Sync.Services;

internal static class OfflineSyncMutations
{
    public static async Task<ApplyModuleProgressResult> CreateProgressAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Guid moduleId,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload,
        CancellationToken ct)
    {
        StudentProgress row = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            ModuleId = moduleId,
            IsCompleted = payload.IsCompleted,
            CompletedAt = payload.IsCompleted ? payload.CompletedAt ?? localTimestamp : payload.CompletedAt,
            TimeOnTaskSeconds = payload.TimeOnTaskSeconds,
            CreatedAt = localTimestamp,
            UpdatedAt = localTimestamp,
        };
        await db.StudentProgresses.AddAsync(row, ct).ConfigureAwait(false);
        return new ApplyModuleProgressResult(row.Id, payload.IsCompleted);
    }

    public static ApplyModuleProgressResult UpdateProgress(
        StudentProgress existing,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload)
    {
        bool newlyCompleted = payload.IsCompleted && !existing.IsCompleted;
        existing.IsCompleted = payload.IsCompleted;
        existing.CompletedAt = payload.IsCompleted ? payload.CompletedAt ?? localTimestamp : payload.CompletedAt;
        existing.TimeOnTaskSeconds = payload.TimeOnTaskSeconds;
        existing.UpdatedAt = localTimestamp;
        return new ApplyModuleProgressResult(existing.Id, newlyCompleted);
    }

    public static async Task<ApplyAssessmentResult> CreateAttemptAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Guid assessmentId,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload,
        CancellationToken ct)
    {
        DateTime submittedAt = payload.SubmittedAt ?? localTimestamp;
        int timeTaken = payload.TimeTakenSeconds ?? 0;
        AssessmentAttempt row = new()
        {
            Id = payload.AttemptId ?? Guid.NewGuid(),
            TenantId = tenantId,
            AssessmentId = assessmentId,
            StudentId = studentId,
            StartedAt = submittedAt.AddSeconds(-Math.Max(0, timeTaken)),
            ScorePercent = payload.ScorePercent ?? 0,
            TimeTakenSeconds = timeTaken,
            SubmittedAt = submittedAt,
            CreatedAt = localTimestamp,
            UpdatedAt = localTimestamp,
        };
        await db.AssessmentAttempts.AddAsync(row, ct).ConfigureAwait(false);
        return new ApplyAssessmentResult(row.Id, true, row.ScorePercent);
    }

    public static ApplyAssessmentResult UpdateAttempt(
        AssessmentAttempt existing,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload)
    {
        bool newlySubmitted = existing.SubmittedAt is null && payload.SubmittedAt is not null;
        existing.ScorePercent = payload.ScorePercent ?? existing.ScorePercent;
        existing.TimeTakenSeconds = payload.TimeTakenSeconds ?? existing.TimeTakenSeconds;
        existing.SubmittedAt = payload.SubmittedAt ?? existing.SubmittedAt;
        existing.UpdatedAt = localTimestamp;
        return new ApplyAssessmentResult(existing.Id, newlySubmitted, existing.ScorePercent);
    }
}

internal sealed record ApplyModuleProgressResult(Guid ProgressId, bool NewlyCompleted);

internal sealed record ApplyAssessmentResult(Guid AttemptId, bool NewlySubmitted, int ScorePercent);
