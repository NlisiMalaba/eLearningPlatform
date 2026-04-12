using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Assessments.Notifications;

public sealed class AssessmentAssignedNotificationHandler : INotificationHandler<AssessmentAssignedNotification>
{
    private readonly IEduZimDbContext _db;
    private readonly ILogger<AssessmentAssignedNotificationHandler> _logger;

    public AssessmentAssignedNotificationHandler(
        IEduZimDbContext db,
        ILogger<AssessmentAssignedNotificationHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(AssessmentAssignedNotification notification, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(notification.TenantId, ct).ConfigureAwait(false);

        AssessmentClassAssignment? assignment = await LoadAssignmentAsync(notification, ct).ConfigureAwait(false);
        if (assignment is null)
            return;

        Assessment? assessment = await LoadAssessmentAsync(notification, assignment, ct).ConfigureAwait(false);
        if (assessment is null)
            return;

        SchoolClass? schoolClass = await LoadSchoolClassAsync(notification, assignment, ct).ConfigureAwait(false);
        if (schoolClass is null)
            return;

        List<Guid> studentIds = await LoadStudentIdsAsync(notification, assignment, ct).ConfigureAwait(false);
        await InsertNotificationsAsync(notification, assessment, schoolClass, assignment, studentIds, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Created {Count} assessment due notifications for assignment {AssignmentId}.",
            studentIds.Count,
            assignment.Id);
    }

    private async Task<AssessmentClassAssignment?> LoadAssignmentAsync(
        AssessmentAssignedNotification notification,
        CancellationToken ct)
    {
        AssessmentClassAssignment? assignment = await _db.AssessmentClassAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == notification.AssessmentClassAssignmentId, ct)
            .ConfigureAwait(false);
        if (assignment is null)
        {
            _logger.LogWarning(
                "Assessment assignment {AssignmentId} not found for notification.",
                notification.AssessmentClassAssignmentId);
        }

        return assignment;
    }

    private async Task<Assessment?> LoadAssessmentAsync(
        AssessmentAssignedNotification notification,
        AssessmentClassAssignment assignment,
        CancellationToken ct)
    {
        return await _db.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assignment.AssessmentId && a.TenantId == notification.TenantId, ct)
            .ConfigureAwait(false);
    }

    private async Task<SchoolClass?> LoadSchoolClassAsync(
        AssessmentAssignedNotification notification,
        AssessmentClassAssignment assignment,
        CancellationToken ct)
    {
        return await _db.SchoolClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == assignment.SchoolClassId && c.TenantId == notification.TenantId, ct)
            .ConfigureAwait(false);
    }

    private async Task<List<Guid>> LoadStudentIdsAsync(
        AssessmentAssignedNotification notification,
        AssessmentClassAssignment assignment,
        CancellationToken ct)
    {
        return await _db.ClassEnrollments
            .AsNoTracking()
            .Where(e => e.SchoolClassId == assignment.SchoolClassId && e.TenantId == notification.TenantId)
            .Select(e => e.StudentUserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task InsertNotificationsAsync(
        AssessmentAssignedNotification notification,
        Assessment assessment,
        SchoolClass schoolClass,
        AssessmentClassAssignment assignment,
        List<Guid> studentIds,
        CancellationToken ct)
    {
        DateTime now = DateTime.UtcNow;
        string message =
            $"""
            Assessment "{assessment.Title}" for class "{schoolClass.Name}" is due by {assignment.DueAtUtc:yyyy-MM-dd HH:mm} UTC.
            """;

        foreach (Guid studentId in studentIds)
        {
            var row = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = notification.TenantId,
                UserId = studentId,
                Type = NotificationType.AssessmentDue,
                Message = message,
                IsRead = false,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Delivered,
                RetryCount = 0,
                CreatedAt = now,
                UpdatedAt = now,
            };
            await _db.Notifications.AddAsync(row, ct).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
