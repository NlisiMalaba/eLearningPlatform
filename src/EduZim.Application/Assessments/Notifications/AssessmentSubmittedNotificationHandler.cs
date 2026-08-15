using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Assessments.Notifications;

/// <summary>Notifies linked parents when a student submits an assessment.</summary>
public sealed class AssessmentSubmittedNotificationHandler : INotificationHandler<AssessmentSubmittedNotification>
{
    private const string FallbackStudentName = "Your student";
    private const string FallbackAssessmentTitle = "an assessment";

    private readonly IEduZimDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<AssessmentSubmittedNotificationHandler> _logger;

    public AssessmentSubmittedNotificationHandler(
        IEduZimDbContext db,
        IMediator mediator,
        ILogger<AssessmentSubmittedNotificationHandler> logger)
    {
        _db = db;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(AssessmentSubmittedNotification notification, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(notification.TenantId, ct).ConfigureAwait(false);

        List<Guid> parentIds = await LoadParentIdsAsync(notification, ct).ConfigureAwait(false);
        if (parentIds.Count == 0)
        {
            _logger.LogDebug(
                "Assessment submitted parent notification skipped; no linked parents for student {StudentId}.",
                notification.StudentId);
            return;
        }

        string studentName = await LoadStudentDisplayNameAsync(notification, ct).ConfigureAwait(false);
        string assessmentTitle = await LoadAssessmentTitleAsync(notification, ct).ConfigureAwait(false);
        string message = AssessmentSubmittedParentMessage.Build(
            studentName,
            assessmentTitle,
            notification.ScorePercent);

        await QueueParentNotificationsAsync(notification.TenantId, parentIds, message, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Queued assessment result notifications for {ParentCount} parent(s) of student {StudentId}.",
            parentIds.Count,
            notification.StudentId);
    }

    private async Task<List<Guid>> LoadParentIdsAsync(
        AssessmentSubmittedNotification notification,
        CancellationToken ct)
    {
        return await _db.ParentStudentLinks
            .AsNoTracking()
            .Where(l => l.TenantId == notification.TenantId && l.StudentUserId == notification.StudentId)
            .Select(l => l.ParentUserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<string> LoadStudentDisplayNameAsync(
        AssessmentSubmittedNotification notification,
        CancellationToken ct)
    {
        string? name = await _db.Users
            .AsNoTracking()
            .Where(
                u => u.Id == notification.StudentId
                    && u.TenantId == notification.TenantId
                    && u.Role == UserRole.Student)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(name) ? FallbackStudentName : name.Trim();
    }

    private async Task<string> LoadAssessmentTitleAsync(
        AssessmentSubmittedNotification notification,
        CancellationToken ct)
    {
        string? title = await _db.Assessments
            .AsNoTracking()
            .Where(a => a.Id == notification.AssessmentId && a.TenantId == notification.TenantId)
            .Select(a => a.Title)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(title) ? FallbackAssessmentTitle : title.Trim();
    }

    private async Task QueueParentNotificationsAsync(
        Guid tenantId,
        List<Guid> parentIds,
        string message,
        CancellationToken ct)
    {
        foreach (Guid parentId in parentIds)
        {
            await _mediator
                .Send(
                    new QueueNotificationCommand(
                        tenantId,
                        parentId,
                        NotificationType.AssessmentSubmitted,
                        message),
                    ct)
                .ConfigureAwait(false);
        }
    }
}
