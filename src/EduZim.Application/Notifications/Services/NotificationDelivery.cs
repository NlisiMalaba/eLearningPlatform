using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Notifications.Services;

internal static class NotificationDelivery
{
    public static async Task<NotificationStatus> SendEmailAsync(
        IEmailService emailService,
        ILogger logger,
        NotificationType type,
        string address,
        string message,
        CancellationToken ct)
    {
        try
        {
            await emailService
                .SendAsync(
                    address,
                    NotificationChannelRules.EmailSubject(type),
                    NotificationChannelRules.EmailHtmlBody(message),
                    ct)
                .ConfigureAwait(false);
            return NotificationStatus.Delivered;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email notification failed for type {Type}.", type);
            return NotificationStatus.Failed;
        }
    }

    public static async Task<NotificationStatus> SendSmsAsync(
        ISmsService smsService,
        ILogger logger,
        NotificationType type,
        string phoneNumber,
        string message,
        CancellationToken ct)
    {
        try
        {
            SmsResult result = await smsService
                .SendAsync(phoneNumber, NotificationChannelRules.TruncateSms(message), ct)
                .ConfigureAwait(false);
            return result.Success ? NotificationStatus.Delivered : NotificationStatus.Failed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMS notification failed for type {Type}.", type);
            return NotificationStatus.Failed;
        }
    }
}
