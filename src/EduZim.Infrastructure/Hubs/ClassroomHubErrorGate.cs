using EduZim.Application.Common.Auth;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace EduZim.Infrastructure.Hubs;

internal static class ClassroomHubErrorGate
{
    public static async Task RunAsync(
        string action,
        Guid sessionId,
        ICurrentUser currentUser,
        IAuditLogWriter audit,
        HubCallerContext context,
        Func<Task> work)
    {
        try
        {
            await work().ConfigureAwait(false);
        }
        catch (TenantAccessViolationException ex)
        {
            await WriteForbiddenAuditAsync(action, sessionId, currentUser, audit, context).ConfigureAwait(false);
            throw new HubException(ex.Message);
        }
        catch (NotFoundException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (ConflictException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    private static async Task WriteForbiddenAuditAsync(
        string action,
        Guid sessionId,
        ICurrentUser currentUser,
        IAuditLogWriter audit,
        HubCallerContext context)
    {
        string? ip = context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
        await audit
            .WriteAsync(
                new AuditLogWrite(
                    currentUser.TenantId,
                    currentUser.UserId,
                    AuthorizationFailureAuditActions.Forbidden,
                    $"ClassroomHub.{action}",
                    sessionId,
                    ip),
                context.ConnectionAborted)
            .ConfigureAwait(false);
    }
}
