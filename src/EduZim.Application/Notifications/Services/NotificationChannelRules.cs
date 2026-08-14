using System.Net;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Application.Notifications.Services;

public static class NotificationChannelRules
{
    public const int SmsMaxLength = 320;

    public static bool IsEnabled(
        NotificationPreference? preference,
        NotificationType type,
        NotificationChannel channel)
    {
        if (preference is not null)
        {
            return channel switch
            {
                NotificationChannel.InApp => preference.InAppEnabled,
                NotificationChannel.Email => preference.EmailEnabled,
                NotificationChannel.Sms => preference.SmsEnabled,
                _ => false,
            };
        }

        return channel switch
        {
            NotificationChannel.InApp => true,
            NotificationChannel.Email => true,
            NotificationChannel.Sms => IsCriticalSmsType(type),
            _ => false,
        };
    }

    public static bool IsCriticalSmsType(NotificationType type) =>
        type is NotificationType.SubscriptionExpiry
            or NotificationType.LiveClassroomReminder
            or NotificationType.InactivityAlert
            or NotificationType.WeeklyProgressSummary;

    public static string EmailSubject(NotificationType type) => type switch
    {
        NotificationType.NewContent => "EduZim: new content assigned",
        NotificationType.AssessmentDue => "EduZim: assessment due",
        NotificationType.BadgeAwarded => "EduZim: badge awarded",
        NotificationType.LiveClassroomReminder => "EduZim: live classroom reminder",
        NotificationType.SubscriptionRenewal => "EduZim: subscription renewal",
        NotificationType.SubscriptionExpiry => "EduZim: subscription expiry",
        NotificationType.InactivityAlert => "EduZim: we miss you",
        NotificationType.MarketplaceAccessRequested => "EduZim: marketplace access requested",
        NotificationType.MarketplacePackRemoved => "EduZim: marketplace pack removed",
        NotificationType.WeeklyProgressSummary => "EduZim: weekly progress summary",
        _ => "EduZim notification",
    };

    public static string EmailHtmlBody(string message) =>
        $"<p>{WebUtility.HtmlEncode(message)}</p>";

    public static string TruncateSms(string message)
    {
        if (message.Length <= SmsMaxLength)
            return message;

        return string.Concat(message.AsSpan(0, SmsMaxLength - 3), "...");
    }
}
