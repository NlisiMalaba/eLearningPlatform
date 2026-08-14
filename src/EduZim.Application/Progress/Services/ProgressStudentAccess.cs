using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Progress.Services;

internal static class ProgressStudentAccess
{
    public static async Task EnsureCanViewAsync(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        Guid tenantId,
        Guid studentId,
        CancellationToken ct)
    {
        bool parentLink = true;
        if (currentUser.Role == UserRole.ParentGuardian)
        {
            parentLink = await db.ParentStudentLinks
                .AsNoTracking()
                .AnyAsync(
                    l => l.TenantId == tenantId
                        && l.ParentUserId == currentUser.UserId
                        && l.StudentUserId == studentId,
                    ct)
                .ConfigureAwait(false);
        }

        TenantAccessHelper.EnsureCanViewStudentProgressData(currentUser, tenantId, studentId, parentLink);
    }
}
