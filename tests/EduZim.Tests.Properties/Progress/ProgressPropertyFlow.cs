using EduZim.Application.Progress.DTOs;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Progress;

internal static class ProgressPropertyFlow
{
    public static (EduZimDbContext Db, IMediator Mediator, IPublisher Publisher, MutableCurrentUser Current)
        Resolve(ServiceProvider provider, IServiceScope scope)
    {
        return (
            scope.ServiceProvider.GetRequiredService<EduZimDbContext>(),
            scope.ServiceProvider.GetRequiredService<IMediator>(),
            scope.ServiceProvider.GetRequiredService<IPublisher>(),
            provider.GetRequiredService<MutableCurrentUser>());
    }

    public static void AsTeacher(MutableCurrentUser current, Guid tenantId)
    {
        current.UserId = Guid.NewGuid();
        current.TenantId = tenantId;
        current.Role = UserRole.Teacher;
    }

    public static void AsStudent(MutableCurrentUser current, Guid tenantId, Guid studentId)
    {
        current.UserId = studentId;
        current.TenantId = tenantId;
        current.Role = UserRole.Student;
    }

    public static void AsParent(MutableCurrentUser current, Guid tenantId, Guid parentId)
    {
        current.UserId = parentId;
        current.TenantId = tenantId;
        current.Role = UserRole.ParentGuardian;
    }

    public static async Task<List<Guid>> SeedLinkedStudentsAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid parentId,
        int count)
    {
        List<Guid> ids = new();
        for (int i = 0; i < count; i++)
        {
            Guid studentId = Guid.NewGuid();
            ids.Add(studentId);
            await ProgressPropertySeeds.SeedStudentAsync(db, tenantId, studentId).ConfigureAwait(false);
            await ProgressPropertySeeds.SeedLinkAsync(db, tenantId, parentId, studentId).ConfigureAwait(false);
        }

        return ids;
    }

    public static void AssertDashboardComplete(LinkedStudentDashboardDto student)
    {
        Assert.True(Enum.IsDefined(student.CurrentGrade));
        Assert.NotNull(student.Subjects);
        Assert.NotNull(student.RecentActivity);
        Assert.InRange(student.OverallProgressPercent, 0, 100);
    }

    public static int OraclePercent(int completed, int total) =>
        (int)Math.Round(100.0 * completed / total, MidpointRounding.AwayFromZero);
}
