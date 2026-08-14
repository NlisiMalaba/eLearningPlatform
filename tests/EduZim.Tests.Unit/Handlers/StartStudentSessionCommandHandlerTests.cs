using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Progress.Commands.StartStudentSession;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class StartStudentSessionCommandHandlerTests
{
    [Fact]
    public async Task Starts_session_when_under_limit()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        List<StudentSession> captured = [];
        StartStudentSessionCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, 3_600),
            sessions: [],
            captured);

        StudentSessionDto dto = await handler.Handle(
            new StartStudentSessionCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.False(dto.LimitReached);
        Assert.Single(captured);
    }

    [Fact]
    public async Task Blocks_new_session_when_daily_limit_reached()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateOnly today = ScreenTimeLimitRules.CalendarDay(DateTime.UtcNow);
        StartStudentSessionCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, 100),
            [
                ProgressTestData.Session(
                    tenantId,
                    studentId,
                    SessionStatus.Paused,
                    today,
                    accumulatedSeconds: 100,
                    lastHeartbeatAt: DateTime.UtcNow),
            ],
            captured: []);

        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(new StartStudentSessionCommand(tenantId, studentId), CancellationToken.None));
    }

    [Fact]
    public async Task Allows_new_session_on_the_next_calendar_day()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateOnly today = ScreenTimeLimitRules.CalendarDay(DateTime.UtcNow);
        List<StudentSession> captured = [];
        StartStudentSessionCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, 100),
            [
                ProgressTestData.Session(
                    tenantId,
                    studentId,
                    SessionStatus.Paused,
                    today.AddDays(-1),
                    accumulatedSeconds: 100,
                    lastHeartbeatAt: DateTime.UtcNow.AddDays(-1)),
            ],
            captured);

        StudentSessionDto dto = await handler.Handle(
            new StartStudentSessionCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.False(dto.LimitReached);
        Assert.Single(captured);
    }

    [Fact]
    public async Task Returns_existing_active_session_for_today()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateOnly today = ScreenTimeLimitRules.CalendarDay(DateTime.UtcNow);
        StudentSession existing = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            20,
            DateTime.UtcNow);
        List<StudentSession> captured = [];
        StartStudentSessionCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, 3_600),
            [existing],
            captured);

        StudentSessionDto dto = await handler.Handle(
            new StartStudentSessionCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(existing.Id, dto.SessionId);
        Assert.Empty(captured);
    }

    [Fact]
    public async Task Student_cannot_start_another_students_session()
    {
        Guid tenantId = Guid.NewGuid();
        StartStudentSessionCommandHandler handler = CreateHandler(
            tenantId,
            Guid.NewGuid(),
            ProgressTestData.Student(tenantId, Guid.NewGuid(), 100),
            sessions: [],
            captured: []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new StartStudentSessionCommand(tenantId, Guid.NewGuid()),
                CancellationToken.None));
    }

    private static StartStudentSessionCommandHandler CreateHandler(
        Guid tenantId,
        Guid userId,
        ApplicationUser student,
        List<StudentSession> sessions,
        List<StudentSession> captured)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(new List<ApplicationUser> { student }.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<StudentSession>> sessionSet = sessions.AsQueryable().BuildMockDbSet();
        sessionSet.Setup(s => s.AddAsync(It.IsAny<StudentSession>(), It.IsAny<CancellationToken>()))
            .Callback<StudentSession, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(ValueTask.FromResult((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<StudentSession>)null!));
        db.Setup(x => x.StudentSessions).Returns(sessionSet.Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Student);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(userId);

        return new StartStudentSessionCommandHandler(
            db.Object,
            user.Object,
            NullLogger<StartStudentSessionCommandHandler>.Instance);
    }
}
