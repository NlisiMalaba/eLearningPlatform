using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Notifications.Commands.QueueNotification;

public sealed class QueueNotificationCommandHandler :
    IRequestHandler<QueueNotificationCommand, Unit>,
    INotificationHandler<StudentInactiveNotification>
{
    private readonly IEduZimDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly INotificationBackgroundJobs _jobs;
    private readonly IMediator _mediator;
    private readonly ILogger<QueueNotificationCommandHandler> _logger;

    public QueueNotificationCommandHandler(
        IEduZimDbContext db,
        IEmailService emailService,
        ISmsService smsService,
        INotificationBackgroundJobs jobs,
        IMediator mediator,
        ILogger<QueueNotificationCommandHandler> logger)
    {
        _db = db;
        _emailService = emailService;
        _smsService = smsService;
        _jobs = jobs;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Unit> Handle(QueueNotificationCommand request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        ApplicationUser user = await LoadUserAsync(request, ct).ConfigureAwait(false);
        NotificationPreference? preference = await LoadPreferenceAsync(request, ct).ConfigureAwait(false);
        DateTime now = DateTime.UtcNow;

        await TryDispatchInAppAsync(request, preference, now, ct).ConfigureAwait(false);
        await TryDispatchEmailAsync(request, user, preference, now, ct).ConfigureAwait(false);
        Guid? failedSmsId = await TryDispatchSmsAsync(request, user, preference, now, ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (failedSmsId is Guid smsId)
            _jobs.ScheduleSmsRetry(request.TenantId, smsId);

        _logger.LogInformation("Queued notification type {Type} for user {UserId}.", request.Type, request.UserId);
        return Unit.Value;
    }

    public Task Handle(StudentInactiveNotification notification, CancellationToken ct) =>
        InactivityAlertFanout.NotifyLinkedParentsAsync(_db, _mediator, _logger, notification, ct);

    private async Task<ApplicationUser> LoadUserAsync(QueueNotificationCommand request, CancellationToken ct)
    {
        ApplicationUser? user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (user is null)
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);

        return user;
    }

    private async Task<NotificationPreference?> LoadPreferenceAsync(
        QueueNotificationCommand request,
        CancellationToken ct)
    {
        return await _db.NotificationPreferences
            .FirstOrDefaultAsync(
                p => p.TenantId == request.TenantId && p.UserId == request.UserId && p.Type == request.Type,
                ct)
            .ConfigureAwait(false);
    }

    private async Task TryDispatchInAppAsync(
        QueueNotificationCommand request,
        NotificationPreference? preference,
        DateTime now,
        CancellationToken ct)
    {
        if (!NotificationChannelRules.IsEnabled(preference, request.Type, NotificationChannel.InApp))
            return;

        await AddRowAsync(request, NotificationChannel.InApp, NotificationStatus.Delivered, now, ct)
            .ConfigureAwait(false);
    }

    private async Task TryDispatchEmailAsync(
        QueueNotificationCommand request,
        ApplicationUser user,
        NotificationPreference? preference,
        DateTime now,
        CancellationToken ct)
    {
        if (!NotificationChannelRules.IsEnabled(preference, request.Type, NotificationChannel.Email))
            return;
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogDebug("Skipping email notification; recipient has no email.");
            return;
        }

        NotificationStatus status = await NotificationDelivery
            .SendEmailAsync(_emailService, _logger, request.Type, user.Email, request.Message, ct)
            .ConfigureAwait(false);
        await AddRowAsync(request, NotificationChannel.Email, status, now, ct).ConfigureAwait(false);
    }

    private async Task<Guid?> TryDispatchSmsAsync(
        QueueNotificationCommand request,
        ApplicationUser user,
        NotificationPreference? preference,
        DateTime now,
        CancellationToken ct)
    {
        if (!NotificationChannelRules.IsEnabled(preference, request.Type, NotificationChannel.Sms))
            return null;
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            _logger.LogDebug("Skipping SMS notification; recipient has no phone number.");
            return null;
        }

        NotificationStatus status = await NotificationDelivery
            .SendSmsAsync(_smsService, _logger, request.Type, user.PhoneNumber, request.Message, ct)
            .ConfigureAwait(false);
        Notification row = await AddRowAsync(request, NotificationChannel.Sms, status, now, ct)
            .ConfigureAwait(false);
        return status == NotificationStatus.Failed ? row.Id : null;
    }

    private async Task<Notification> AddRowAsync(
        QueueNotificationCommand request,
        NotificationChannel channel,
        NotificationStatus status,
        DateTime now,
        CancellationToken ct)
    {
        var row = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            Type = request.Type,
            Message = request.Message,
            IsRead = false,
            Channel = channel,
            Status = status,
            RetryCount = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _db.Notifications.AddAsync(row, ct).ConfigureAwait(false);
        return row;
    }
}
