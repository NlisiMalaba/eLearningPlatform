using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Gamification.Notifications;

/// <summary>Awards points when a module is completed or an assessment is submitted.</summary>
public sealed class AwardPointsCommandHandler :
    INotificationHandler<ModuleCompletedNotification>,
    INotificationHandler<AssessmentSubmittedNotification>
{
    private readonly IEduZimDbContext _db;
    private readonly ILogger<AwardPointsCommandHandler> _logger;

    public AwardPointsCommandHandler(IEduZimDbContext db, ILogger<AwardPointsCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task Handle(ModuleCompletedNotification notification, CancellationToken ct) =>
        AwardAsync(notification.TenantId, notification.StudentId, PointsAwardRules.ForModuleCompletion(), ct);

    public Task Handle(AssessmentSubmittedNotification notification, CancellationToken ct) =>
        AwardAsync(
            notification.TenantId,
            notification.StudentId,
            PointsAwardRules.ForAssessment(notification.ScorePercent),
            ct);

    private async Task AwardAsync(Guid tenantId, Guid studentId, int points, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(tenantId, ct).ConfigureAwait(false);

        StudentPoints? record = await _db.StudentPoints
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.StudentId == studentId, ct)
            .ConfigureAwait(false);

        DateTime utcNow = DateTime.UtcNow;
        if (record is null)
            await AddNewRecordAsync(tenantId, studentId, points, utcNow, ct).ConfigureAwait(false);
        else
            IncrementExisting(record, points, utcNow);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Awarded {Points} points to student {StudentId} in tenant {TenantId}.",
            points,
            studentId,
            tenantId);
    }

    private async Task AddNewRecordAsync(
        Guid tenantId,
        Guid studentId,
        int points,
        DateTime utcNow,
        CancellationToken ct)
    {
        StudentPoints created = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            TotalPoints = points,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        await _db.StudentPoints.AddAsync(created, ct).ConfigureAwait(false);
    }

    private static void IncrementExisting(StudentPoints record, int points, DateTime utcNow)
    {
        record.TotalPoints += points;
        record.UpdatedAt = utcNow;
    }
}
