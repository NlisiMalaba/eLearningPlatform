using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.Progress.Services;

internal static class ParentDashboardAccess
{
    public static void EnsureCanView(ICurrentUser user, Guid tenantId, Guid parentUserId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;

        TenantAccessHelper.EnsureCanAccessTenantScope(user, tenantId);
        if (user.Role is UserRole.Teacher or UserRole.SchoolAdmin)
            return;

        if (user.Role == UserRole.ParentGuardian)
        {
            if (user.UserId != parentUserId)
            {
                throw new TenantAccessViolationException(
                    "Parents may only view their own dashboard.",
                    tenantId,
                    parentUserId);
            }

            return;
        }

        throw new TenantAccessViolationException(
            "This role cannot view the parent dashboard.",
            tenantId,
            parentUserId);
    }
}
