using System.Globalization;
using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.AdaptiveLearning.Services;

public static class AdaptiveLearningProfileCacheUpdater
{
    public static async Task ApplyFromSubmittedAssessmentAsync(
        ICacheService cache,
        Guid tenantId,
        Guid studentId,
        Guid assessmentId,
        Guid moduleId,
        int scorePercent,
        StudentLearningProfileDto? existingProfile,
        CancellationToken ct)
    {
        int tier = existingProfile?.DifficultyTier ?? 3;
        DateTime utcNow = DateTime.UtcNow;
        if (scorePercent < 70)
            tier = Math.Max(1, tier - 1);
        else if (scorePercent >= 85)
            tier = Math.Min(5, tier + 1);

        StudentLearningProfileDto profile = new(
            studentId,
            utcNow,
            assessmentId,
            moduleId,
            scorePercent,
            tier);

        string key = AdaptiveLearningCacheKeys.LearningProfile(tenantId, studentId);
        await cache.SetAsync(key, profile, TimeSpan.FromDays(30), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the cached learning profile from this module's latest assessment scores
    /// and invalidates the weekly summary so time-on-task is recomputed.
    /// </summary>
    public static async Task ApplyFromCompletedModuleAsync(
        IEduZimDbContext db,
        ICacheService cache,
        Guid tenantId,
        Guid studentId,
        Guid moduleId,
        CancellationToken ct)
    {
        string key = AdaptiveLearningCacheKeys.LearningProfile(tenantId, studentId);
        StudentLearningProfileDto? existing = await cache.GetAsync<StudentLearningProfileDto>(key, ct)
            .ConfigureAwait(false);

        AssessmentAttempt? latest = await LoadLatestModuleAttemptAsync(db, tenantId, studentId, moduleId, ct)
            .ConfigureAwait(false);

        if (latest is not null)
        {
            await ApplyFromSubmittedAssessmentAsync(
                    cache,
                    tenantId,
                    studentId,
                    latest.AssessmentId,
                    moduleId,
                    latest.ScorePercent,
                    existing,
                    ct)
                .ConfigureAwait(false);
        }
        else
        {
            StudentLearningProfileDto profile = new(
                studentId,
                DateTime.UtcNow,
                existing?.LastAssessmentId ?? Guid.Empty,
                moduleId,
                existing?.LastScorePercent ?? 0,
                existing?.DifficultyTier ?? 3);
            await cache.SetAsync(key, profile, TimeSpan.FromDays(30), ct).ConfigureAwait(false);
        }

        await InvalidateCurrentWeeklySummaryAsync(cache, tenantId, studentId, ct).ConfigureAwait(false);
    }

    public static async Task InvalidateCurrentWeeklySummaryAsync(
        ICacheService cache,
        Guid tenantId,
        Guid studentId,
        CancellationToken ct)
    {
        DateTime referenceUtc = DateTime.UtcNow;
        int isoYear = ISOWeek.GetYear(referenceUtc);
        int isoWeek = ISOWeek.GetWeekOfYear(referenceUtc);
        string weeklyKey = AdaptiveLearningCacheKeys.WeeklySummary(tenantId, studentId, isoYear, isoWeek);
        await cache.RemoveAsync(weeklyKey, ct).ConfigureAwait(false);
    }

    private static async Task<AssessmentAttempt?> LoadLatestModuleAttemptAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Guid moduleId,
        CancellationToken ct)
    {
        List<Guid> assessmentIds = await db.Assessments
            .AsNoTracking()
            .Where(a => a.ModuleId == moduleId && a.TenantId == tenantId)
            .Select(a => a.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (assessmentIds.Count == 0)
            return null;

        return await db.AssessmentAttempts
            .AsNoTracking()
            .Where(
                a => a.TenantId == tenantId
                    && a.StudentId == studentId
                    && a.SubmittedAt != null
                    && assessmentIds.Contains(a.AssessmentId))
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>Rebuilds the learning-profile cache from the student's latest submitted attempt, if any.</summary>
    /// <returns><see langword="true"/> when a submitted attempt was found and the profile was updated.</returns>
    public static async Task<bool> TryRefreshFromLatestSubmittedAttemptAsync(
        IEduZimDbContext db,
        ICacheService cache,
        Guid tenantId,
        Guid studentId,
        CancellationToken ct)
    {
        AssessmentAttempt? latest = await db.AssessmentAttempts
            .AsNoTracking()
            .Where(
                a => a.TenantId == tenantId
                    && a.StudentId == studentId
                    && a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (latest is null)
            return false;

        Assessment? assessment = await db.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == latest.AssessmentId && a.TenantId == tenantId, ct)
            .ConfigureAwait(false);
        if (assessment is null)
            return false;

        string key = AdaptiveLearningCacheKeys.LearningProfile(tenantId, studentId);
        StudentLearningProfileDto? existing = await cache.GetAsync<StudentLearningProfileDto>(key, ct)
            .ConfigureAwait(false);

        await ApplyFromSubmittedAssessmentAsync(
                cache,
                tenantId,
                studentId,
                latest.AssessmentId,
                assessment.ModuleId,
                latest.ScorePercent,
                existing,
                ct)
            .ConfigureAwait(false);

        return true;
    }
}
