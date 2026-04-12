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

        await AdaptiveLearningProfileCacheUpdater.ApplyFromSubmittedAssessmentAsync(
                _cache,
                notification.TenantId,
                notification.StudentId,
                notification.AssessmentId,
                assessment.ModuleId,
                notification.ScorePercent,
                existing,
                ct)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "Updated adaptive learning profile for student {StudentId} after assessment {AssessmentId}.",
            notification.StudentId,
            notification.AssessmentId);
    }
}
