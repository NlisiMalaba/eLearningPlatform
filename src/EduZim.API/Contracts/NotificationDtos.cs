using System.ComponentModel.DataAnnotations;
using EduZim.Application.Notifications.DTOs;

namespace EduZim.API.Contracts;

public sealed class UpdateNotificationPreferencesRequest
{
    [Required]
    [MinLength(1)]
    public List<NotificationPreferenceItemDto> Preferences { get; set; } = default!;
}
