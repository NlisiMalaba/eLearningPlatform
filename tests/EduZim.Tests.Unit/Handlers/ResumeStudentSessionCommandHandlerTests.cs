using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Progress.Commands.ResumeStudentSession;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ResumeStudentSessionCommandHandlerTests
{
    [Fact]
    public async Task Resumes_session_paused_for_inactivity()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Paused,
            today,
            accumulatedSeconds: 40,
            lastHeartbeatAt: utcNow.AddSeconds(-10),
            lastInteractionAt: utcNow.AddSeconds(-70));
        ResumeStudentSessionCommandHandler handler = CreateHandler(tenantId, studentId, session, limitSeconds: null);

        StudentSessionDto dto = await handler.Handle(
            new ResumeStudentSessionCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.False(dto.RestPromptRequired);
        Assert.False(dto.ResumePromptRequired);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.False(session.RestPromptRequired);
    }

    [Fact]
    public async Task Clears_rest_prompt_and_starts_a_new_segment()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        DateTime oldSegment = utcNow.AddMinutes(-25);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 40,
            lastHeartbeatAt: utcNow,
            lastInteractionAt: utcNow,
            segmentStartedAt: oldSegment,
            restPromptRequired: true);
        ResumeStudentSessionCommandHandler handler = CreateHandler(tenantId, studentId, session, limitSeconds: null);

        StudentSessionDto dto = await handler.Handle(
            new ResumeStudentSessionCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.False(dto.RestPromptRequired);
        Assert.False(session.RestPromptRequired);
        Assert.True(session.SegmentStartedAt >= utcNow.AddSeconds(-2));
        Assert.NotEqual(oldSegment, session.SegmentStartedAt);
    }

    [Fact]
    public async Task Rejects_resume_when_daily_screen_time_limit_is_reached()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Paused,
            today,
            accumulatedSeconds: 100,
            lastHeartbeatAt: utcNow);
        ResumeStudentSessionCommandHandler handler = CreateHandler(tenantId, studentId, session, limitSeconds: 100);

        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(new ResumeStudentSessionCommand(tenantId, studentId), CancellationToken.None));
        Assert.Equal(SessionStatus.Paused, session.Status);
    }

    private static ResumeStudentSessionCommandHandler CreateHandler(
        Guid tenantId,
        Guid studentId,
        StudentSession session,
        int? limitSeconds)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users)
            .Returns(new List<ApplicationUser> { ProgressTestData.Student(tenantId, studentId, limitSeconds) }
                .AsQueryable()
                .BuildMockDbSet()
                .Object);
        db.Setup(x => x.StudentSessions)
            .Returns(new List<StudentSession> { session }.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Student);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(studentId);

        return new ResumeStudentSessionCommandHandler(
            db.Object,
            user.Object,
            NullLogger<ResumeStudentSessionCommandHandler>.Instance);
    }
}
