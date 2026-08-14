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

    [Fact]
    public async Task Pauses_preschool_session_after_sixty_seconds_without_interaction()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 10,
            lastHeartbeatAt: utcNow.AddSeconds(-5),
            lastInteractionAt: utcNow.AddSeconds(-60));
        RecordStudentSessionHeartbeatCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, dailyLimitSeconds: null),
            [session],
            [ProgressTestData.ActiveTenant(tenantId, TenantTier.PreSchool)]);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentSessionHeartbeatCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Paused, dto.Status);
        Assert.False(dto.LimitReached);
        Assert.True(dto.ResumePromptRequired);
        Assert.Equal(SessionStatus.Paused, session.Status);
    }

    [Fact]
    public async Task Leaves_preschool_session_active_when_idle_under_sixty_seconds()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 10,
            lastHeartbeatAt: utcNow,
            lastInteractionAt: utcNow.AddSeconds(-20));
        RecordStudentSessionHeartbeatCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, dailyLimitSeconds: null),
            [session],
            [ProgressTestData.ActiveTenant(tenantId, TenantTier.PreSchool)]);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentSessionHeartbeatCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.False(dto.ResumePromptRequired);
    }

    [Fact]
    public async Task Sets_rest_prompt_after_twenty_minutes_of_continuous_preschool_session()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 10,
            lastHeartbeatAt: utcNow,
            lastInteractionAt: utcNow.AddSeconds(-10),
            segmentStartedAt: utcNow.AddSeconds(-PreschoolSessionRules.RestPromptAfterSeconds));
        RecordStudentSessionHeartbeatCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, dailyLimitSeconds: null),
            [session],
            [ProgressTestData.ActiveTenant(tenantId, TenantTier.PreSchool)]);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentSessionHeartbeatCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.True(dto.RestPromptRequired);
        Assert.True(session.RestPromptRequired);
        Assert.True(dto.ContinuousInteractionSeconds >= PreschoolSessionRules.RestPromptAfterSeconds);
    }

    [Fact]
    public async Task Does_not_pause_school_session_for_inactivity()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 10,
            lastHeartbeatAt: utcNow,
            lastInteractionAt: utcNow.AddSeconds(-120));
        RecordStudentSessionHeartbeatCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            ProgressTestData.Student(tenantId, studentId, dailyLimitSeconds: null),
            [session],
            [ProgressTestData.ActiveTenant(tenantId)]);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentSessionHeartbeatCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.False(dto.ResumePromptRequired);
        Assert.False(dto.RestPromptRequired);
    }

    private static RecordStudentSessionHeartbeatCommandHandler CreateHandler(
        Guid tenantId,
        Guid studentId,
        ApplicationUser student,
        List<StudentSession> sessions,
        List<Tenant>? tenants = null)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(new List<ApplicationUser> { student }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.StudentSessions).Returns(sessions.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Tenants).Returns((tenants ?? []).AsQueryable().BuildMockDbSet().Object);

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
