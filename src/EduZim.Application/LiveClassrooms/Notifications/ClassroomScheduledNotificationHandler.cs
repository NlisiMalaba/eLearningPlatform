using EduZim.Application.Common.Interfaces;
using EduZim.Application.LiveClassrooms.Services;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.LiveClassrooms.Notifications;

public sealed class ClassroomScheduledNotificationHandler : INotificationHandler<ClassroomScheduledNotification>
{
    private readonly IEduZimDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<ClassroomScheduledNotificationHandler> _logger;

    public ClassroomScheduledNotificationHandler(
        IEduZimDbContext db,
        IMediator mediator,
        ILogger<ClassroomScheduledNotificationHandler> logger)
    {
        _db = db;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(ClassroomScheduledNotification notification, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(notification.TenantId, ct).ConfigureAwait(false);
        ClassroomSession? session = await LoadSessionAsync(notification, ct).ConfigureAwait(false);
        if (session is null)
            return;

        SchoolClass? schoolClass = await LoadClassAsync(notification, session, ct).ConfigureAwait(false);
        if (schoolClass is null)
            return;

        List<Guid> studentIds = await LoadStudentIdsAsync(notification, session, ct).ConfigureAwait(false);
        List<Guid> recipients = await LoadRecipientIdsAsync(notification, studentIds, ct).ConfigureAwait(false);
        string message = ClassroomReminderMessage.Build(schoolClass.Name, session.StartAtUtc);
        await QueueRemindersAsync(notification.TenantId, recipients, message, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Queued live classroom reminders for {Count} recipient(s) for session {SessionId}.",
            recipients.Count,
            session.Id);
    }

    private async Task<ClassroomSession?> LoadSessionAsync(
        ClassroomScheduledNotification notification,
        CancellationToken ct)
    {
        return await _db.ClassroomSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == notification.SessionId && s.TenantId == notification.TenantId, ct)
            .ConfigureAwait(false);
    }

    private async Task<SchoolClass?> LoadClassAsync(
        ClassroomScheduledNotification notification,
        ClassroomSession session,
        CancellationToken ct)
    {
        return await _db.SchoolClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == session.SchoolClassId && c.TenantId == notification.TenantId, ct)
            .ConfigureAwait(false);
    }

    private async Task<List<Guid>> LoadStudentIdsAsync(
        ClassroomScheduledNotification notification,
        ClassroomSession session,
        CancellationToken ct)
    {
        return await _db.ClassEnrollments
            .AsNoTracking()
            .Where(e => e.SchoolClassId == session.SchoolClassId && e.TenantId == notification.TenantId)
            .Select(e => e.StudentUserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<List<Guid>> LoadRecipientIdsAsync(
        ClassroomScheduledNotification notification,
        List<Guid> studentIds,
        CancellationToken ct)
    {
        if (studentIds.Count == 0)
            return [];

        List<Guid> parentIds = await _db.ParentStudentLinks
            .AsNoTracking()
            .Where(l => l.TenantId == notification.TenantId && studentIds.Contains(l.StudentUserId))
            .Select(l => l.ParentUserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return studentIds.Concat(parentIds).Distinct().ToList();
    }

    private async Task QueueRemindersAsync(
        Guid tenantId,
        List<Guid> recipients,
        string message,
        CancellationToken ct)
    {
        foreach (Guid userId in recipients)
        {
            await _mediator
                .Send(
                    new QueueNotificationCommand(
                        tenantId,
                        userId,
                        NotificationType.LiveClassroomReminder,
                        message),
                    ct)
                .ConfigureAwait(false);
        }
    }
}
