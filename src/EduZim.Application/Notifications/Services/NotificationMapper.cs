using EduZim.Application.Notifications.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Application.Notifications.Services;

public static class NotificationMapper
{
    public static InAppNotificationDto ToInAppDto(Notification notification) =>
        new(notification.Id, notification.Type, notification.Message, notification.IsRead, notification.CreatedAt);

    public static IReadOnlyList<NotificationPreferenceItemDto> ToPreferenceDtos(
        IReadOnlyList<NotificationPreference> stored)
    {
        Dictionary<NotificationType, NotificationPreference> byType =
            stored.ToDictionary(p => p.Type);

        return Enum.GetValues<NotificationType>()
            .Select(type => MapPreferenceItem(type, byType.GetValueOrDefault(type)))
            .ToList();
    }

    private static NotificationPreferenceItemDto MapPreferenceItem(
        NotificationType type,
        NotificationPreference? stored)
    {
        if (stored is not null)
            return new NotificationPreferenceItemDto(
                type,
                stored.InAppEnabled,
                stored.EmailEnabled,
                stored.SmsEnabled);

        return new NotificationPreferenceItemDto(
            type,
            NotificationChannelRules.IsEnabled(null, type, NotificationChannel.InApp),
            NotificationChannelRules.IsEnabled(null, type, NotificationChannel.Email),
            NotificationChannelRules.IsEnabled(null, type, NotificationChannel.Sms));
    }
}
