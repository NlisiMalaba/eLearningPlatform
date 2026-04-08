using System.Net.Mime;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;

namespace EduZim.API.Middleware;

public sealed class TenantMiddleware
{
    private const string TenantSuspendedType = "https://eduzim.co.zw/errors/tenant-suspended";
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserInitializer currentUserInitializer,
        ICurrentUser currentUser,
        ITenantLifecycleChecker tenantLifecycle)
    {
        currentUserInitializer.InitializeFromPrincipal(context.User);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        if (currentUser.Role == UserRole.PlatformAdmin)
        {
            await _next(context);
            return;
        }

        if (currentUser.TenantId is not { } tenantId)
        {
            await _next(context);
            return;
        }

        if (await tenantLifecycle.IsTenantSuspendedAsync(tenantId, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsJsonAsync(
                new
                {
                    type = TenantSuspendedType,
                    title = "Subscription Suspended",
                    status = StatusCodes.Status402PaymentRequired,
                    detail = "Your school's subscription has expired. Please contact your administrator.",
                },
                context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
