using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Progress.Services;

internal static class ScreenTimeAccess
{
    public static async Task EnsureCanSetLimitAsync(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        Guid tenantId,
        Guid studentId,
        CancellationToken ct)
    {
        if (currentUser.Role == UserRole.PlatformAdmin)
            return;

        TenantAccessHelper.EnsureCanAccessTenantScope(currentUser, tenantId);
        if (currentUser.Role is UserRole.SchoolAdmin)
            return;

        if (currentUser.Role == UserRole.ParentGuardian)
        {
            await EnsureParentLinkedAsync(db, currentUser.UserId, tenantId, studentId, ct)
                .ConfigureAwait(false);
            return;
        }

        throw new TenantAccessViolationException(
            "This role cannot set a student's screen time limit.",
            tenantId,
            studentId);
    }

    public static void EnsureCanOperateSession(ICurrentUser currentUser, Guid tenantId, Guid studentId)
    {
        if (currentUser.Role == UserRole.PlatformAdmin)
            return;

        TenantAccessHelper.EnsureCanAccessTenantScope(currentUser, tenantId);
        if (currentUser.Role != UserRole.Student || currentUser.UserId != studentId)
        {
            throw new TenantAccessViolationException(
                "Only the student can start or continue a learning session.",
                tenantId,
                studentId);
        }
    }

    private static async Task EnsureParentLinkedAsync(
        IEduZimDbContext db,
        Guid parentId,
        Guid tenantId,
        Guid studentId,
        CancellationToken ct)
    {
        bool linked = await db.ParentStudentLinks
            .AsNoTracking()
            .AnyAsync(
                l => l.TenantId == tenantId
                    && l.ParentUserId == parentId
                    && l.StudentUserId == studentId,
                ct)
            .ConfigureAwait(false);
        if (!linked)
        {
            throw new TenantAccessViolationException(
                "Parents may only set screen time limits for linked students.",
                tenantId,
                studentId);
        }
    }
}
