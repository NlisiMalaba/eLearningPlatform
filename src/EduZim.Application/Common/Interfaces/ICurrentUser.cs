using EduZim.Domain.Enums;

namespace EduZim.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid? TenantId { get; }
    UserRole Role { get; }
}
