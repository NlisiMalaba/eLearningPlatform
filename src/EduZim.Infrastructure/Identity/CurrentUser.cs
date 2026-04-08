using System.Security.Claims;
using EduZim.Application.Common.Auth;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;

namespace EduZim.Infrastructure.Identity;

public sealed class CurrentUser : ICurrentUser, ICurrentUserInitializer
{
    public Guid UserId { get; private set; }

    public Guid? TenantId { get; private set; }

    public UserRole Role { get; private set; }

    public void InitializeFromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            UserId = Guid.Empty;
            TenantId = null;
            Role = UserRole.Student;
            return;
        }

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        UserId = Guid.TryParse(sub, out var uid) ? uid : Guid.Empty;

        var tenantClaim = principal.FindFirstValue(EduZimClaimTypes.TenantId);
        TenantId = Guid.TryParse(tenantClaim, out var tid) ? tid : null;

        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value
            ?? principal.FindFirst("role")?.Value;
        Role = Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role)
            ? role
            : UserRole.Student;
    }
}
