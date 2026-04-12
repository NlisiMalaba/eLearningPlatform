using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.AdaptiveLearning.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.AdaptiveLearning.Notifications;

/// <summary>Updates cached learning profile when an assessment attempt is submitted.</summary>
public sealed class UpdateLearningProfileCommandHandler : INotificationHandler<AssessmentSubmittedNotification>
{
    private readonly IEduZimDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateLearningProfileCommandHandler> _logger;

    public UpdateLearningProfileCommandHandler(
        IEduZimDbContext db,
        ICacheService cache,
        ILogger<UpdateLearningProfileCommandHandler> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(AssessmentSubmittedNotification notification, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(notification.TenantId, ct).ConfigureAwait(false);

        Assessment? assessment = await _db.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == notification.AssessmentId && a.TenantId == notification.TenantId, ct)
            .ConfigureAwait(false);
        if (assessment is null)
        {
            _logger.LogWarning(
                "Assessment {AssessmentId} not found; skipping learning profile cache update.",
                notification.AssessmentId);
            return;
        }

        string key = AdaptiveLearningCacheKeys.LearningProfile(notification.TenantId, notification.StudentId);
        StudentLearningProfileDto? existing = await _cache.GetAsync<StudentLearningProfileDto>(key, ct)
            .ConfigureAwait(false);

        int tier = existing?.DifficultyTier ?? 3;
        DateTime utcNow = DateTime.UtcNow;
        if (notification.ScorePercent < 70)
            tier = Math.Max(1, tier - 1);
        else if (notification.ScorePercent >= 85)
            tier = Math.Min(5, tier + 1);

        var profile = new StudentLearningProfileDto(
            notification.StudentId,
            utcNow,
            notification.AssessmentId,
            assessment.ModuleId,
            notification.ScorePercent,
            tier);

        await _cache.SetAsync(key, profile, TimeSpan.FromDays(30), ct).ConfigureAwait(false);

        _logger.LogDebug(
            "Updated adaptive learning profile for student {StudentId} (tier {Tier}).",
            notification.StudentId,
            tier);
    }
}
