using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.ZimBot.Services;

public static class ZimBotAccess
{
    public static void EnsureCanChat(ICurrentUser user, Guid tenantId, Guid studentId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;

        TenantAccessHelper.EnsureCanAccessTenantScope(user, tenantId);
        if (user.Role == UserRole.Student && user.UserId == studentId)
            return;

        throw new TenantAccessViolationException(
            "Only the student may send ZimBot chat messages.",
            tenantId,
            studentId);
    }

    public static void EnsureCanReviewLogs(ICurrentUser user, Guid tenantId) =>
        TenantAccessHelper.EnsureCanViewTenantDashboard(user, tenantId);
}
