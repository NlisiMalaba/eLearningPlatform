using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class NotificationPreference : TenantEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
}
