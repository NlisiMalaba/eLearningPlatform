using EduZim.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace EduZim.Domain.Entities;

/// <summary>
/// Extends <see cref="IdentityUser{TKey}"/>; email verification, lockout, and sign-in failures use
/// <see cref="IdentityUser{TKey}.EmailConfirmed"/>, <see cref="IdentityUser{TKey}.LockoutEnd"/>, and
/// <see cref="IdentityUser{TKey}.AccessFailedCount"/>.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    public UserRole Role { get; set; }
    public string? PreferredLanguage { get; set; }
}
