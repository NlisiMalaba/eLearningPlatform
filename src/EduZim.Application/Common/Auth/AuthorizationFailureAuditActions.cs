namespace EduZim.Application.Common.Auth;

/// <summary>Action names written by <c>AuditMiddleware</c> for failed authorisation responses.</summary>
public static class AuthorizationFailureAuditActions
{
    public const string Forbidden = "Authorization.Forbidden";
    public const string Unauthorized = "Authorization.Unauthorized";
}
