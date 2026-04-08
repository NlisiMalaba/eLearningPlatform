using System.Security.Claims;

namespace EduZim.Application.Common.Interfaces;

public interface ICurrentUserInitializer
{
    void InitializeFromPrincipal(ClaimsPrincipal? principal);

    /// <summary>Populates the current user from <c>HttpContext.User</c> on the active request.</summary>
    void InitializeFromHttpContext();
}
