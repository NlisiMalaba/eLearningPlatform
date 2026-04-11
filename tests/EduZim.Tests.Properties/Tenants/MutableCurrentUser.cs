using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Properties.Tenants;

internal sealed class MutableCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    public UserRole Role { get; set; }
}
