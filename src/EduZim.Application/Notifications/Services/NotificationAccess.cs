using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.Notifications.Services;

internal static class NotificationAccess
{
    public static void EnsureCanView(ICurrentUser user, Guid tenantId, Guid targetUserId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;

        TenantAccessHelper.EnsureCanAccessTenantScope(user, tenantId);
        if (user.UserId == targetUserId)
            return;
        if (user.Role == UserRole.SchoolAdmin)
            return;

        throw new TenantAccessViolationException(
            "You may only view your own notifications.",
            tenantId,
            targetUserId);
    }

    public static void EnsureCanUpdatePreferences(ICurrentUser user, Guid tenantId, Guid targetUserId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;

        TenantAccessHelper.EnsureCanAccessTenantScope(user, tenantId);
        if (user.UserId == targetUserId)
            return;

        throw new TenantAccessViolationException(
            "You may only update your own notification preferences.",
            tenantId,
            targetUserId);
    }
}
