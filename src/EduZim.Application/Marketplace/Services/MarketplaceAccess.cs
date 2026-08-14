using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.Marketplace.Services;

public static class MarketplaceAccess
{
    public static void EnsureCanSubmit(ICurrentUser user, Guid tenantId) =>
        TenantAccessHelper.EnsureCanManageSchoolContent(user, tenantId);

    public static void EnsureCanBrowse(ICurrentUser user)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        if (user.TenantId is null)
        {
            throw new TenantAccessViolationException(
                "Tenant context is required to browse the marketplace.",
                null,
                null);
        }

        if (user.Role is UserRole.Teacher or UserRole.SchoolAdmin)
            return;

        throw new TenantAccessViolationException(
            "Only teachers and school administrators can browse the marketplace.",
            user.TenantId,
            null);
    }

    public static void EnsureCanRequestAccess(ICurrentUser user, Guid requestingTenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        TenantAccessHelper.EnsureCanAccessTenantScope(user, requestingTenantId);
        if (user.Role is UserRole.Teacher or UserRole.SchoolAdmin)
            return;

        throw new TenantAccessViolationException(
            "Only teachers and school administrators can request marketplace access.",
            requestingTenantId,
            null);
    }

    public static void EnsureCanApproveAccess(ICurrentUser user, ContentPack pack)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        TenantAccessHelper.EnsureCanAccessTenantScope(user, pack.TenantId);
        if (user.UserId == pack.SubmittedByUserId)
            return;
        if (user.Role == UserRole.SchoolAdmin)
            return;

        throw new TenantAccessViolationException(
            "Only the originating teacher can approve access to this content pack.",
            pack.TenantId,
            pack.Id);
    }

    public static void EnsureCanRate(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        TenantAccessHelper.EnsureCanAccessTenantScope(user, tenantId);
        if (user.Role is UserRole.Teacher or UserRole.SchoolAdmin)
            return;

        throw new TenantAccessViolationException(
            "Only teachers and school administrators can rate marketplace packs.",
            tenantId,
            null);
    }

    public static bool CanReadPackContent(
        ICurrentUser user,
        ContentPack pack,
        ContentPackAccessRequest? grant)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return true;
        if (user.TenantId == pack.TenantId)
            return true;
        return grant is { Status: ContentPackAccessStatus.Approved };
    }
}
