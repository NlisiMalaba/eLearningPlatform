using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.Identity.Services;

internal static class UserPreferenceAccess
{
    public static void EnsureCanUpdateFontSize(ICurrentUser user, Guid targetUserId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;

        if (user.UserId == targetUserId)
            return;

        throw new TenantAccessViolationException(
            "You may only update your own font size preference.",
            user.TenantId,
            targetUserId);
    }
}
