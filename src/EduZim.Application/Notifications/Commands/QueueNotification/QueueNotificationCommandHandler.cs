using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;
using EduZim.Application.Exceptions;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Notifications.Commands.QueueNotification;

public sealed class QueueNotificationCommandHandler : IRequestHandler<QueueNotificationCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<QueueNotificationCommandHandler> _logger;

    public QueueNotificationCommandHandler(
        IEduZimDbContext db,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<QueueNotificationCommandHandler> logger)
    {
        _db = db;
        _emailService = emailService;
        _smsService = smsService;
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
        await TryDispatchSmsAsync(request, user, preference, now, ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Queued notification type {Type} for user {UserId}.", request.Type, request.UserId);
        return Unit.Value;
    }

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

        NotificationStatus status = await SendEmailAsync(request, user.Email, ct).ConfigureAwait(false);
        await AddRowAsync(request, NotificationChannel.Email, status, now, ct).ConfigureAwait(false);
    }

    private async Task TryDispatchSmsAsync(
        QueueNotificationCommand request,
        ApplicationUser user,
        NotificationPreference? preference,
        DateTime now,
        CancellationToken ct)
    {
        if (!NotificationChannelRules.IsEnabled(preference, request.Type, NotificationChannel.Sms))
            return;
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            _logger.LogDebug("Skipping SMS notification; recipient has no phone number.");
            return;
        }

        NotificationStatus status = await SendSmsAsync(request, user.PhoneNumber, ct).ConfigureAwait(false);
        await AddRowAsync(request, NotificationChannel.Sms, status, now, ct).ConfigureAwait(false);
    }

    private async Task<NotificationStatus> SendEmailAsync(
        QueueNotificationCommand request,
        string email,
        CancellationToken ct)
    {
        try
        {
            await _emailService
                .SendAsync(
                    email,
                    NotificationChannelRules.EmailSubject(request.Type),
                    NotificationChannelRules.EmailHtmlBody(request.Message),
                    ct)
                .ConfigureAwait(false);
            return NotificationStatus.Delivered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email notification failed for type {Type}.", request.Type);
            return NotificationStatus.Failed;
        }
    }

    private async Task<NotificationStatus> SendSmsAsync(
        QueueNotificationCommand request,
        string phoneNumber,
        CancellationToken ct)
    {
        try
        {
            SmsResult result = await _smsService
                .SendAsync(phoneNumber, NotificationChannelRules.TruncateSms(request.Message), ct)
                .ConfigureAwait(false);
            return result.Success ? NotificationStatus.Delivered : NotificationStatus.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS notification failed for type {Type}.", request.Type);
            return NotificationStatus.Failed;
        }
    }

    private async Task AddRowAsync(
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
    }
}
