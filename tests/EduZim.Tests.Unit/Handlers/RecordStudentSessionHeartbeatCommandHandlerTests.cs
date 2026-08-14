using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.Commands.RecordStudentSessionHeartbeat;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class RecordStudentSessionHeartbeatCommandHandlerTests
{
    [Fact]
    public async Task Pauses_session_when_daily_limit_is_reached()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateOnly today = ScreenTimeLimitRules.CalendarDay(DateTime.UtcNow);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 90,
            lastHeartbeatAt: DateTime.UtcNow.AddSeconds(-20));
        RecordStudentSessionHeartbeatCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, 100),
            [session]);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentSessionHeartbeatCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Paused, dto.Status);
        Assert.True(dto.LimitReached);
        Assert.Equal(100, session.AccumulatedSeconds);
        Assert.Equal(SessionStatus.Paused, session.Status);
    }

    [Fact]
    public async Task Leaves_session_active_when_under_limit()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateOnly today = ScreenTimeLimitRules.CalendarDay(DateTime.UtcNow);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 10,
            lastHeartbeatAt: DateTime.UtcNow);
        RecordStudentSessionHeartbeatCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, 3_600),
            [session]);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentSessionHeartbeatCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.False(dto.LimitReached);
    }

    [Fact]
    public async Task Does_not_enforce_when_no_limit_is_set()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateOnly today = ScreenTimeLimitRules.CalendarDay(DateTime.UtcNow);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 50_000,
            lastHeartbeatAt: DateTime.UtcNow.AddSeconds(-100));
        RecordStudentSessionHeartbeatCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, dailyLimitSeconds: null),
            [session]);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentSessionHeartbeatCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.False(dto.LimitReached);
        Assert.True(session.AccumulatedSeconds >= 50_000);
    }

    private static RecordStudentSessionHeartbeatCommandHandler CreateHandler(
        Guid tenantId,
        Guid studentId,
        ApplicationUser student,
        List<StudentSession> sessions)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(new List<ApplicationUser> { student }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.StudentSessions).Returns(sessions.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Student);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(studentId);

        return new RecordStudentSessionHeartbeatCommandHandler(
            db.Object,
            user.Object,
            NullLogger<RecordStudentSessionHeartbeatCommandHandler>.Instance);
    }
}
