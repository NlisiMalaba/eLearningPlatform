using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.Commands.SetDailyScreenTimeLimit;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class SetDailyScreenTimeLimitCommandHandlerTests
{
    [Fact]
    public async Task Linked_parent_sets_limit()
    {
        Guid tenantId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ApplicationUser student = ProgressTestData.Student(tenantId, studentId);
        SetDailyScreenTimeLimitCommandHandler handler = CreateHandler(
            tenantId,
            parentId,
            UserRole.ParentGuardian,
            student,
            [ProgressTestData.Link(tenantId, parentId, studentId)],
            sessions: []);

        ScreenTimeSettingsDto dto = await handler.Handle(
            new SetDailyScreenTimeLimitCommand(tenantId, studentId, 3_600),
            CancellationToken.None);

        Assert.Equal(3_600, dto.DailyScreenTimeLimitSeconds);
        Assert.Equal(3_600, student.DailyScreenTimeLimitSeconds);
    }

    [Fact]
    public async Task Unlinked_parent_is_forbidden()
    {
        Guid tenantId = Guid.NewGuid();
        SetDailyScreenTimeLimitCommandHandler handler = CreateHandler(
            tenantId,
            Guid.NewGuid(),
            UserRole.ParentGuardian,
            ProgressTestData.Student(tenantId, Guid.NewGuid()),
            links: [],
            sessions: []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new SetDailyScreenTimeLimitCommand(tenantId, Guid.NewGuid(), 600),
                CancellationToken.None));
    }

    [Fact]
    public async Task Setting_limit_pauses_active_session_when_already_over()
    {
        Guid tenantId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateOnly today = ScreenTimeLimitRules.CalendarDay(DateTime.UtcNow);
        StudentSession active = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 500,
            lastHeartbeatAt: DateTime.UtcNow);
        SetDailyScreenTimeLimitCommandHandler handler = CreateHandler(
            tenantId,
            parentId,
            UserRole.ParentGuardian,
            ProgressTestData.Student(tenantId, studentId),
            [ProgressTestData.Link(tenantId, parentId, studentId)],
            [active]);

        await handler.Handle(new SetDailyScreenTimeLimitCommand(tenantId, studentId, 100), CancellationToken.None);

        Assert.Equal(SessionStatus.Paused, active.Status);
    }

    private static SetDailyScreenTimeLimitCommandHandler CreateHandler(
        Guid tenantId,
        Guid userId,
        UserRole role,
        ApplicationUser student,
        List<ParentStudentLink> links,
        List<StudentSession> sessions)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(new List<ApplicationUser> { student }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ParentStudentLinks).Returns(links.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.StudentSessions).Returns(sessions.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(userId);

        return new SetDailyScreenTimeLimitCommandHandler(
            db.Object,
            user.Object,
            NullLogger<SetDailyScreenTimeLimitCommandHandler>.Instance);
    }
}
