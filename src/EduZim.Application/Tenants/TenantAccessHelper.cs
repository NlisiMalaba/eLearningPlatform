using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.Tenants;

public static class TenantAccessHelper
{
    public static void EnsureCanManageTenantSettings(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        if (user.TenantId != tenantId || user.Role != UserRole.SchoolAdmin)
            throw new TenantAccessViolationException(
                "You are not allowed to manage branding for this tenant.",
                tenantId,
                tenantId);
    }

    public static void EnsureCanViewTenantDashboard(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        if (user.TenantId != tenantId)
            throw new TenantAccessViolationException(
                "You are not allowed to view this tenant's dashboard.",
                tenantId,
                tenantId);
        if (user.Role is not (UserRole.SchoolAdmin or UserRole.Teacher))
            throw new TenantAccessViolationException(
                "You are not allowed to view this tenant's dashboard.",
                tenantId,
                tenantId);
    }

    public static void EnsureCanAccessTenantScope(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        if (user.TenantId != tenantId)
            throw new TenantAccessViolationException(
                "You do not have access to this tenant.",
                tenantId,
                tenantId);
    }

    public static void EnsurePlatformAdmin(ICurrentUser user)
    {
        if (user.Role != UserRole.PlatformAdmin)
            throw new TenantAccessViolationException(
                "This operation requires platform administrator privileges.",
                user.TenantId,
                null);
    }

    /// <summary>Creating or archiving curriculum content: school admin, teacher, or platform admin for the tenant.</summary>
    public static void EnsureCanManageSchoolContent(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        if (user.TenantId != tenantId)
            throw new TenantAccessViolationException(
                "You do not have access to content for this tenant.",
                tenantId,
                tenantId);
        if (user.Role is not (UserRole.SchoolAdmin or UserRole.Teacher))
            throw new TenantAccessViolationException(
                "You are not allowed to manage content for this tenant.",
                tenantId,
                tenantId);
    }

    /// <summary>Adaptive learning views for a student: teacher, school admin, the student, or a linked parent.</summary>
    public static void EnsureCanViewStudentAdaptiveData(
        ICurrentUser user,
        Guid tenantId,
        Guid studentUserId,
        bool parentIsLinkedToStudent)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        EnsureCanAccessTenantScope(user, tenantId);
        if (user.Role == UserRole.Student)
        {
            if (user.UserId != studentUserId)
            {
                throw new TenantAccessViolationException(
                    "Students may only view their own adaptive learning data.",
                    tenantId,
                    studentUserId);
            }

            return;
        }

        if (user.Role is UserRole.Teacher or UserRole.SchoolAdmin)
            return;
        if (user.Role == UserRole.ParentGuardian)
        {
            if (!parentIsLinkedToStudent)
            {
                throw new TenantAccessViolationException(
                    "Parents may only view adaptive data for linked students.",
                    tenantId,
                    studentUserId);
            }

            return;
        }

        throw new TenantAccessViolationException(
            "This role cannot view adaptive learning data for this student.",
            tenantId,
            studentUserId);
    }

    /// <summary>Billing (subscriptions, invoices): school admin, parent/guardian, or platform admin for the tenant.</summary>
    public static void EnsureCanManageBilling(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        if (user.TenantId != tenantId)
            throw new TenantAccessViolationException(
                "You do not have access to billing for this tenant.",
                tenantId,
                tenantId);
        if (user.Role is not (UserRole.SchoolAdmin or UserRole.ParentGuardian))
            throw new TenantAccessViolationException(
                "You are not allowed to manage billing for this tenant.",
                tenantId,
                tenantId);
    }

    /// <summary>School leaderboard: students, teachers, and school admins in the tenant (requirement 9.5).</summary>
    public static void EnsureCanViewLeaderboard(ICurrentUser user, Guid tenantId)
    {
        if (user.Role == UserRole.PlatformAdmin)
            return;
        EnsureCanAccessTenantScope(user, tenantId);
        if (user.Role is UserRole.Student or UserRole.Teacher or UserRole.SchoolAdmin)
            return;

        throw new TenantAccessViolationException(
            "This role cannot view the tenant leaderboard.",
            tenantId,
            tenantId);
    }
}
