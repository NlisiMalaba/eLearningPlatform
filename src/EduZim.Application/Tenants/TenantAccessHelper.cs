using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.Tenants;

internal static class TenantAccessHelper
{
    public static void EnsureCanManageTenantSettings(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        if (user.TenantId != tenantId || user.Role != UserRole.SchoolAdmin)
            throw new TenantAccessViolationException(
                "You are not allowed to manage branding for this tenant.",
                tenantId,
                tenantId);
    }

    public static void EnsureCanViewTenantDashboard(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        if (user.TenantId != tenantId)
            throw new TenantAccessViolationException(
                "You are not allowed to view this tenant's dashboard.",
                tenantId,
                tenantId);
        if (user.Role is not (UserRole.SchoolAdmin or UserRole.Teacher))
            throw new TenantAccessViolationException(
                "You are not allowed to view this tenant's dashboard.",
                tenantId,
                tenantId);
    }

    public static void EnsureCanAccessTenantScope(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        if (user.TenantId != tenantId)
            throw new TenantAccessViolationException(
                "You do not have access to this tenant.",
                tenantId,
                tenantId);
    }

    public static void EnsurePlatformAdmin(ICurrentUser user)
    {
        if (user.Role != UserRole.PlatformAdmin)
            throw new TenantAccessViolationException(
                "This operation requires platform administrator privileges.",
                user.TenantId,
                null);
    }
}
