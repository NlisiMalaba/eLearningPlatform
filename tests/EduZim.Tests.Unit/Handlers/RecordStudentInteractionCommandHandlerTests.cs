using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.Commands.RecordStudentInteraction;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class RecordStudentInteractionCommandHandlerTests
{
    [Fact]
    public async Task Records_interaction_and_keeps_session_active()
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
            accumulatedSeconds: 30,
            lastHeartbeatAt: utcNow,
            lastInteractionAt: utcNow.AddSeconds(-20));
        RecordStudentInteractionCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            session,
            TenantTier.PreSchool);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentInteractionCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(SessionStatus.Active, dto.Status);
        Assert.False(dto.RestPromptRequired);
        Assert.True(session.LastInteractionAt >= utcNow.AddSeconds(-2));
    }

    [Fact]
    public async Task Sets_rest_prompt_after_twenty_minutes_of_continuous_interaction()
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
            accumulatedSeconds: 30,
            lastHeartbeatAt: utcNow,
            lastInteractionAt: utcNow.AddSeconds(-10),
            segmentStartedAt: utcNow.AddSeconds(-PreschoolSessionRules.RestPromptAfterSeconds));
        RecordStudentInteractionCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            session,
            TenantTier.PreSchool);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentInteractionCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.True(dto.RestPromptRequired);
        Assert.True(session.RestPromptRequired);
        Assert.Equal(SessionStatus.Active, dto.Status);
    }

    [Fact]
    public async Task Does_not_set_rest_prompt_for_school_tier()
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
            accumulatedSeconds: 30,
            lastHeartbeatAt: utcNow,
            lastInteractionAt: utcNow,
            segmentStartedAt: utcNow.AddSeconds(-PreschoolSessionRules.RestPromptAfterSeconds));
        RecordStudentInteractionCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            session,
            TenantTier.School);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentInteractionCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.False(dto.RestPromptRequired);
        Assert.False(session.RestPromptRequired);
    }

    [Fact]
    public async Task Keeps_rest_prompt_until_session_is_resumed()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        DateTime segmentStart = utcNow.AddSeconds(-PreschoolSessionRules.RestPromptAfterSeconds);
        StudentSession session = ProgressTestData.Session(
            tenantId,
            studentId,
            SessionStatus.Active,
            today,
            accumulatedSeconds: 30,
            lastHeartbeatAt: utcNow,
            lastInteractionAt: utcNow.AddSeconds(-5),
            segmentStartedAt: segmentStart,
            restPromptRequired: true);
        RecordStudentInteractionCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            session,
            TenantTier.PreSchool);

        StudentSessionDto dto = await handler.Handle(
            new RecordStudentInteractionCommand(tenantId, studentId),
            CancellationToken.None);

        Assert.True(dto.RestPromptRequired);
        Assert.Equal(segmentStart, session.SegmentStartedAt);
    }

    private static RecordStudentInteractionCommandHandler CreateHandler(
        Guid tenantId,
        Guid studentId,
        StudentSession session,
        TenantTier tier)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users)
            .Returns(new List<ApplicationUser> { ProgressTestData.Student(tenantId, studentId) }
                .AsQueryable()
                .BuildMockDbSet()
                .Object);
        db.Setup(x => x.StudentSessions)
            .Returns(new List<StudentSession> { session }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Tenants)
            .Returns(new List<Tenant> { ProgressTestData.ActiveTenant(tenantId, tier) }
                .AsQueryable()
                .BuildMockDbSet()
                .Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Student);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(studentId);

        return new RecordStudentInteractionCommandHandler(
            db.Object,
            user.Object,
            NullLogger<RecordStudentInteractionCommandHandler>.Instance);
    }
}
