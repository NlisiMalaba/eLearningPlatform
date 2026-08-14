using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.Sync.Services;

public static class OfflineSyncAccess
{
    public static void EnsureCanUpload(ICurrentUser user, Guid tenantId, Guid studentId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;

        TenantAccessHelper.EnsureCanAccessTenantScope(user, tenantId);
        if (user.Role == UserRole.Student && user.UserId == studentId)
            return;

        throw new TenantAccessViolationException(
            "Only the student may upload their offline sync queue.",
            tenantId,
            studentId);
    }
}
