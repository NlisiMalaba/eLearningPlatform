using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Queries.GetParentDashboard;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetParentDashboardQueryHandlerTests
{
    [Fact]
    public async Task Returns_required_fields_for_each_linked_student()
    {
        Guid tenantId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        DateTime completedAt = DateTime.UtcNow.AddDays(-1);
        GetParentDashboardQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.ParentGuardian,
            parentId,
            [ProgressTestData.Link(tenantId, parentId, studentId)],
            [ProgressTestData.Module(tenantId, moduleId, "Math", 1, GradeLevel.Grade2, "Fractions")],
            [ProgressTestData.Progress(tenantId, studentId, moduleId, true, true, completedAt)],
            [ProgressTestData.Badge(tenantId, studentId, BadgeType.FirstModule, completedAt)]);

        ParentDashboardDto dto = await handler.Handle(
            new GetParentDashboardQuery(tenantId, parentId),
            CancellationToken.None);

        LinkedStudentDashboardDto student = Assert.Single(dto.Students);
        Assert.Equal(studentId, student.StudentId);
        Assert.Equal(GradeLevel.Grade2, student.CurrentGrade);
        Assert.Contains("Math", student.Subjects);
        Assert.Equal(100, student.OverallProgressPercent);
        Assert.Contains(student.RecentActivity, a => a.Kind == "ModuleCompleted" && a.Title == "Fractions");
        Assert.Contains(student.RecentActivity, a => a.Kind == "BadgeEarned");
    }

    [Fact]
    public async Task Parent_cannot_view_another_parents_dashboard()
    {
        Guid tenantId = Guid.NewGuid();
        GetParentDashboardQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.ParentGuardian,
            Guid.NewGuid(),
            links: [],
            modules: [],
            progresses: [],
            badges: []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetParentDashboardQuery(tenantId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Returns_empty_students_when_parent_has_no_links()
    {
        Guid tenantId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        GetParentDashboardQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.ParentGuardian,
            parentId,
            links: [],
            modules: [],
            progresses: [],
            badges: []);

        ParentDashboardDto dto = await handler.Handle(
            new GetParentDashboardQuery(tenantId, parentId),
            CancellationToken.None);

        Assert.Empty(dto.Students);
    }

    private static GetParentDashboardQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<ParentStudentLink> links,
        List<Module> modules,
        List<StudentProgress> progresses,
        List<Badge> badges)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.ParentStudentLinks).Returns(links.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Modules).Returns(modules.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.StudentProgresses).Returns(progresses.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Badges).Returns(badges.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(userId);

        return new GetParentDashboardQueryHandler(
            db.Object,
            user.Object,
            NullLogger<GetParentDashboardQueryHandler>.Instance);
    }
}
