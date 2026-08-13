using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.LiveClassrooms.Services;

public static class LiveClassroomAccess
{
    public static void EnsureCanSchedule(ICurrentUser user, Guid tenantId) =>
        TenantAccessHelper.EnsureCanManageSchoolContent(user, tenantId);

    public static void EnsureCanEnd(ICurrentUser user, Guid tenantId, ClassroomSession session)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        TenantAccessHelper.EnsureCanAccessTenantScope(user, tenantId);
        if (user.UserId == session.TeacherUserId)
            return;
        if (user.Role == UserRole.SchoolAdmin)
            return;

        throw new TenantAccessViolationException(
            "You are not allowed to end this classroom session.",
            tenantId,
            session.Id);
    }

    public static void EnsureCanJoin(ICurrentUser user, Guid tenantId, ClassroomSession session, bool isEnrolled)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        TenantAccessHelper.EnsureCanAccessTenantScope(user, tenantId);
        if (user.UserId == session.TeacherUserId)
            return;
        if (user.Role == UserRole.SchoolAdmin)
            return;
        if (user.Role == UserRole.Student && isEnrolled)
            return;

        throw new TenantAccessViolationException(
            "You are not allowed to join this classroom session.",
            tenantId,
            session.Id);
    }

    public static void EnsureCanViewRecording(
        ICurrentUser user,
        Guid tenantId,
        ClassroomSession session,
        bool isEnrolled) =>
        EnsureCanJoin(user, tenantId, session, isEnrolled);

    public static void EnsureCanViewAttendance(ICurrentUser user, Guid tenantId, ClassroomSession session) =>
        EnsureCanEnd(user, tenantId, session);
}
