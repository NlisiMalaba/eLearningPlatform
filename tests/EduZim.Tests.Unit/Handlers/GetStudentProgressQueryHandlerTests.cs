using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Queries.GetStudentProgress;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetStudentProgressQueryHandlerTests
{
    [Fact]
    public async Task Returns_per_subject_percent_and_unlocks_next_after_completion()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        GetStudentProgressQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            Guid.NewGuid(),
            [
                ProgressTestData.Module(tenantId, first, "Math", 1),
                ProgressTestData.Module(tenantId, second, "Math", 2),
            ],
            [ProgressTestData.Progress(tenantId, studentId, first, completed: true, unlocked: true, DateTime.UtcNow)]);

        StudentProgressDto dto = await handler.Handle(
            new GetStudentProgressQuery(tenantId, studentId),
            CancellationToken.None);

        SubjectProgressDto subject = Assert.Single(dto.Subjects);
        Assert.Equal("Math", subject.Subject);
        Assert.Equal(50, subject.ProgressPercent);
        Assert.True(subject.Modules.Single(m => m.ModuleId == first).IsCompleted);
        Assert.True(subject.Modules.Single(m => m.ModuleId == first).IsAccessible);
        Assert.False(subject.Modules.Single(m => m.ModuleId == second).IsCompleted);
        Assert.True(subject.Modules.Single(m => m.ModuleId == second).IsAccessible);
    }

    [Fact]
    public async Task First_module_is_accessible_before_any_progress()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        GetStudentProgressQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [
                ProgressTestData.Module(tenantId, first, "Math", 1),
                ProgressTestData.Module(tenantId, second, "Math", 2),
            ],
            progresses: []);

        StudentProgressDto dto = await handler.Handle(
            new GetStudentProgressQuery(tenantId, studentId),
            CancellationToken.None);

        Assert.True(dto.Subjects[0].Modules.Single(m => m.ModuleId == first).IsAccessible);
        Assert.False(dto.Subjects[0].Modules.Single(m => m.ModuleId == second).IsAccessible);
        Assert.Equal(0, dto.Subjects[0].ProgressPercent);
    }

    [Fact]
    public async Task Student_cannot_view_another_students_progress()
    {
        Guid tenantId = Guid.NewGuid();
        GetStudentProgressQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            modules: [],
            progresses: []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetStudentProgressQuery(tenantId, Guid.NewGuid()), CancellationToken.None));
    }

    private static GetStudentProgressQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<Module> modules,
        List<StudentProgress> progresses)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Modules).Returns(modules.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.StudentProgresses).Returns(progresses.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ParentStudentLinks)
            .Returns(new List<ParentStudentLink>().AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(userId);

        return new GetStudentProgressQueryHandler(
            db.Object,
            user.Object,
            NullLogger<GetStudentProgressQueryHandler>.Instance);
    }
}
