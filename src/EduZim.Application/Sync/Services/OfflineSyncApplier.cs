using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Sync.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Sync.Services;

internal static class OfflineSyncApplier
{
    public static DateTime ServerTimestamp(StudentProgress progress) =>
        progress.CompletedAt ?? progress.UpdatedAt;

    public static DateTime ServerTimestamp(AssessmentAttempt attempt) =>
        attempt.SubmittedAt ?? attempt.UpdatedAt;

    public static async Task<StudentProgress?> FindProgressAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Guid moduleId,
        CancellationToken ct)
    {
        return await db.StudentProgresses
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId && p.StudentId == studentId && p.ModuleId == moduleId,
                ct)
            .ConfigureAwait(false);
    }

    public static async Task<AssessmentAttempt?> FindAttemptAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        OfflineSyncPayloadDto payload,
        CancellationToken ct)
    {
        Guid assessmentId = payload.AssessmentId
            ?? throw new DomainException("Assessment attempt sync payload is missing AssessmentId.");
        if (payload.AttemptId is Guid attemptId)
        {
            return await db.AssessmentAttempts
                .FirstOrDefaultAsync(
                    a => a.Id == attemptId && a.TenantId == tenantId && a.StudentId == studentId,
                    ct)
                .ConfigureAwait(false);
        }

        return await db.AssessmentAttempts
            .Where(a => a.TenantId == tenantId && a.StudentId == studentId && a.AssessmentId == assessmentId)
            .OrderByDescending(a => a.SubmittedAt ?? a.UpdatedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public static async Task<ApplyModuleProgressResult> ApplyModuleProgressAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload,
        CancellationToken ct)
    {
        Guid moduleId = payload.ModuleId
            ?? throw new DomainException("Module progress sync payload is missing ModuleId.");
        await EnsureExistsAsync(db.Modules, tenantId, moduleId, nameof(Module), ct).ConfigureAwait(false);

        StudentProgress? existing = await FindProgressAsync(db, tenantId, studentId, moduleId, ct)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return await OfflineSyncMutations
                .CreateProgressAsync(db, tenantId, studentId, moduleId, localTimestamp, payload, ct)
                .ConfigureAwait(false);
        }

        return OfflineSyncMutations.UpdateProgress(existing, localTimestamp, payload);
    }

    public static async Task<ApplyAssessmentResult> ApplyAssessmentAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload,
        CancellationToken ct)
    {
        Guid assessmentId = payload.AssessmentId
            ?? throw new DomainException("Assessment attempt sync payload is missing AssessmentId.");
        await EnsureExistsAsync(db.Assessments, tenantId, assessmentId, nameof(Assessment), ct)
            .ConfigureAwait(false);

        AssessmentAttempt? existing = await FindAttemptAsync(db, tenantId, studentId, payload, ct)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return await OfflineSyncMutations
                .CreateAttemptAsync(db, tenantId, studentId, assessmentId, localTimestamp, payload, ct)
                .ConfigureAwait(false);
        }

        return OfflineSyncMutations.UpdateAttempt(existing, localTimestamp, payload);
    }

    private static async Task EnsureExistsAsync<T>(
        DbSet<T> set,
        Guid tenantId,
        Guid id,
        string name,
        CancellationToken ct)
        where T : TenantEntity
    {
        bool exists = await set.AsNoTracking().AnyAsync(e => e.Id == id && e.TenantId == tenantId, ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(name, id);
    }
}
