using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;

namespace EduZim.API.Middleware;

public sealed class AuditMiddleware
{
    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser, IAuditLogWriter auditLogWriter)
    {
        await _next(context);

        var ip = context.Connection.RemoteIpAddress?.ToString();
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        var code = context.Response.StatusCode;
        if (code is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            var userId = currentUser.UserId != Guid.Empty
                ? currentUser.UserId
                : Guid.Empty;
            var action = code == StatusCodes.Status401Unauthorized
                ? "Authorization.Unauthorized"
                : "Authorization.Forbidden";
            await auditLogWriter.WriteAsync(
                new AuditLogWrite(
                    currentUser.TenantId,
                    userId,
                    action,
                    path,
                    null,
                    ip));
            return;
        }

        if (code >= StatusCodes.Status400BadRequest)
            return;

        if (!MutatingMethods.Contains(method))
            return;

        if (currentUser.UserId == Guid.Empty)
            return;

        if (currentUser.Role is not (UserRole.SchoolAdmin or UserRole.PlatformAdmin))
            return;

        await auditLogWriter.WriteAsync(
            new AuditLogWrite(
                currentUser.TenantId,
                currentUser.UserId,
                $"{method} {path}",
                "Administrative",
                null,
                ip));
    }
}
