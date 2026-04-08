using System.Security.Claims;

namespace EduZim.Application.Common.Interfaces;

public interface ICurrentUserInitializer
{
    void InitializeFromPrincipal(ClaimsPrincipal? principal);
}
