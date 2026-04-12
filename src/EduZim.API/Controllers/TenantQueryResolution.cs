using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

internal static class TenantQueryResolution
{
    public static IActionResult? ValidateTenantQuery(ControllerBase http, ICurrentUser currentUser, Guid? tenantId)
    {
        if (currentUser.Role == UserRole.PlatformAdmin)
        {
            if (tenantId is null)
            {
                return http.Problem(
                    title: "Tenant required",
                    detail: "Provide tenantId (query) when using platform administrator credentials.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return null;
        }

        if (currentUser.TenantId is null)
            return http.Forbid();

        if (tenantId.HasValue && tenantId.Value != currentUser.TenantId.Value)
        {
            return http.Problem(
                title: "Tenant mismatch",
                detail: "tenantId does not match the authenticated user's tenant.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        return null;
    }

    public static Guid ResolveTenantId(ICurrentUser currentUser, Guid? tenantId)
    {
        return currentUser.Role == UserRole.PlatformAdmin
            ? tenantId!.Value
            : currentUser.TenantId!.Value;
    }
}
