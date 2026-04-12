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
