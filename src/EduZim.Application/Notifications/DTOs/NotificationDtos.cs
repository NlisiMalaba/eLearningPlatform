using EduZim.Domain.Enums;

namespace EduZim.Application.Notifications.DTOs;

public sealed record InAppNotificationDto(
    Guid Id,
    NotificationType Type,
    string Message,
    bool IsRead,
    DateTime CreatedAt);

public sealed record InAppNotificationsDto(
    Guid UserId,
    IReadOnlyList<InAppNotificationDto> Items);

public sealed record NotificationPreferenceItemDto(
    NotificationType Type,
    bool InAppEnabled,
    bool EmailEnabled,
    bool SmsEnabled);

public sealed record NotificationPreferencesDto(
    Guid UserId,
    IReadOnlyList<NotificationPreferenceItemDto> Preferences);
