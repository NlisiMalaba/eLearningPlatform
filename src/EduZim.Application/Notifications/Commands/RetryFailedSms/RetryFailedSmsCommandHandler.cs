using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Notifications.Commands.RetryFailedSms;

public sealed class RetryFailedSmsCommandHandler : IRequestHandler<RetryFailedSmsCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ISmsService _smsService;
    private readonly INotificationBackgroundJobs _jobs;
    private readonly ILogger<RetryFailedSmsCommandHandler> _logger;

    public RetryFailedSmsCommandHandler(
        IEduZimDbContext db,
        ISmsService smsService,
        INotificationBackgroundJobs jobs,
        ILogger<RetryFailedSmsCommandHandler> logger)
    {
        _db = db;
        _smsService = smsService;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task<Unit> Handle(RetryFailedSmsCommand request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        Notification? notification = await LoadRetryableSmsAsync(request, ct).ConfigureAwait(false);
        if (notification is null)
            return Unit.Value;

        if (notification.RetryCount >= SmsRetryRules.MaxRetryAttempts)
        {
            MarkUndelivered(notification);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Unit.Value;
        }

        await AttemptRetryAsync(notification, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        ScheduleIfNeeded(notification);
        return Unit.Value;
    }

    private async Task<Notification?> LoadRetryableSmsAsync(RetryFailedSmsCommand request, CancellationToken ct)
    {
        Notification? notification = await _db.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == request.NotificationId
                    && n.TenantId == request.TenantId
                    && n.Channel == NotificationChannel.Sms,
                ct)
            .ConfigureAwait(false);
        if (notification is null)
        {
            _logger.LogWarning(
                "SMS retry skipped: notification {NotificationId} not found.",
                request.NotificationId);
            return null;
        }

        if (notification.Status is NotificationStatus.Delivered or NotificationStatus.Undelivered)
            return null;
        if (notification.Status != NotificationStatus.Failed)
            return null;

        return notification;
    }

    private async Task AttemptRetryAsync(Notification notification, CancellationToken ct)
    {
        string? phoneNumber = await LoadPhoneNumberAsync(notification, ct).ConfigureAwait(false);
        NotificationStatus sendStatus = string.IsNullOrWhiteSpace(phoneNumber)
            ? NotificationStatus.Failed
            : await NotificationDelivery
                .SendSmsAsync(_smsService, _logger, notification.Type, phoneNumber, notification.Message, ct)
                .ConfigureAwait(false);

        notification.RetryCount++;
        notification.UpdatedAt = DateTime.UtcNow;
        if (sendStatus == NotificationStatus.Delivered)
        {
            notification.Status = NotificationStatus.Delivered;
            _logger.LogInformation(
                "SMS retry {RetryCount} delivered for notification {NotificationId}.",
                notification.RetryCount,
                notification.Id);
            return;
        }

        if (notification.RetryCount >= SmsRetryRules.MaxRetryAttempts)
        {
            MarkUndelivered(notification);
            return;
        }

        notification.Status = NotificationStatus.Failed;
        _logger.LogWarning(
            "SMS retry {RetryCount} failed for notification {NotificationId}.",
            notification.RetryCount,
            notification.Id);
    }

    private async Task<string?> LoadPhoneNumberAsync(Notification notification, CancellationToken ct)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == notification.UserId && u.TenantId == notification.TenantId)
            .Select(u => u.PhoneNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private void ScheduleIfNeeded(Notification notification)
    {
        if (notification.Status != NotificationStatus.Failed)
            return;
        if (!SmsRetryRules.CanScheduleAnotherRetry(notification.RetryCount))
            return;

        _jobs.ScheduleSmsRetry(notification.TenantId, notification.Id);
    }

    private void MarkUndelivered(Notification notification)
    {
        notification.Status = NotificationStatus.Undelivered;
        notification.UpdatedAt = DateTime.UtcNow;
        _logger.LogWarning(
            "SMS notification {NotificationId} marked undelivered after {RetryCount} retries.",
            notification.Id,
            notification.RetryCount);
    }
}
