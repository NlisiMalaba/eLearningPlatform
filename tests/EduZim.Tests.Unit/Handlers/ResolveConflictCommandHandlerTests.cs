using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Sync.Commands.ResolveConflict;
using EduZim.Application.Sync.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static EduZim.Tests.Unit.Handlers.SyncTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ResolveConflictCommandHandlerTests
{
    [Fact]
    public async Task Local_later_timestamp_is_retained_and_conflict_is_logged()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        DateTime serverTs = DateTime.UtcNow.AddHours(-2);
        DateTime localTs = DateTime.UtcNow;
        StudentProgress existing = Progress(tenantId, studentId, moduleId, serverTs, timeOnTaskSeconds: 10);
        List<SyncConflictLog> logs = [];
        List<ModuleCompletedNotification> published = [];
        ResolveConflictCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            [existing],
            [],
            [ModuleRow(tenantId, moduleId)],
            logs,
            published);

        ResolveConflictResultDto result = await handler.Handle(
            new ResolveConflictCommand(
                tenantId,
                studentId,
                Guid.NewGuid(),
                localTs,
                ModulePayload(moduleId, timeOnTaskSeconds: 90, completedAt: localTs)),
            CancellationToken.None);

        Assert.True(result.LocalWon);
        Assert.Equal(localTs, result.RetainedTimestamp);
        Assert.True(existing.IsCompleted);
        Assert.Equal(90, existing.TimeOnTaskSeconds);
        Assert.Equal(localTs, existing.UpdatedAt);
        SyncConflictLog log = Assert.Single(logs);
        Assert.True(log.LocalWon);
        Assert.Equal(serverTs, log.ServerTimestamp);
        Assert.Equal(localTs, log.LocalTimestamp);
        Assert.Equal("ModuleProgress", log.ResourceType);
        Assert.Single(published);
    }

    [Fact]
    public async Task Server_later_timestamp_is_retained_and_conflict_is_logged()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        DateTime localTs = DateTime.UtcNow.AddHours(-2);
        DateTime serverTs = DateTime.UtcNow;
        StudentProgress existing = Progress(
            tenantId,
            studentId,
            moduleId,
            serverTs,
            isCompleted: true,
            timeOnTaskSeconds: 40);
        List<SyncConflictLog> logs = [];
        ResolveConflictCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            [existing],
            [],
            [ModuleRow(tenantId, moduleId)],
            logs,
            []);

        ResolveConflictResultDto result = await handler.Handle(
            new ResolveConflictCommand(
                tenantId,
                studentId,
                Guid.NewGuid(),
                localTs,
                ModulePayload(moduleId, timeOnTaskSeconds: 999, completedAt: localTs)),
            CancellationToken.None);

        Assert.False(result.LocalWon);
        Assert.Equal(serverTs, result.RetainedTimestamp);
        Assert.Equal(40, existing.TimeOnTaskSeconds);
        SyncConflictLog log = Assert.Single(logs);
        Assert.False(log.LocalWon);
        Assert.Equal(serverTs, log.RetainedTimestamp);
    }

    [Fact]
    public async Task Equal_timestamps_retain_the_server_record()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        DateTime timestamp = DateTime.UtcNow;
        StudentProgress existing = Progress(tenantId, studentId, moduleId, timestamp, timeOnTaskSeconds: 15);
        List<SyncConflictLog> logs = [];
        ResolveConflictCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            [existing],
            [],
            [ModuleRow(tenantId, moduleId)],
            logs,
            []);

        ResolveConflictResultDto result = await handler.Handle(
            new ResolveConflictCommand(
                tenantId,
                studentId,
                Guid.NewGuid(),
                timestamp,
                ModulePayload(moduleId, timeOnTaskSeconds: 80, completedAt: timestamp)),
            CancellationToken.None);

        Assert.False(result.LocalWon);
        Assert.Equal(15, existing.TimeOnTaskSeconds);
        Assert.Single(logs);
    }

    [Fact]
    public async Task Local_later_assessment_attempt_is_applied()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid assessmentId = Guid.NewGuid();
        DateTime serverTs = DateTime.UtcNow.AddMinutes(-30);
        DateTime localTs = DateTime.UtcNow;
        AssessmentAttempt existing = Attempt(tenantId, studentId, assessmentId, serverTs, scorePercent: 50);
        List<SyncConflictLog> logs = [];
        ResolveConflictCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            [],
            [existing],
            [],
            logs,
            [],
            [AssessmentRow(tenantId, assessmentId, Guid.NewGuid())]);

        ResolveConflictResultDto result = await handler.Handle(
            new ResolveConflictCommand(
                tenantId,
                studentId,
                Guid.NewGuid(),
                localTs,
                AttemptPayload(assessmentId, 92, 180, localTs, existing.Id)),
            CancellationToken.None);

        Assert.True(result.LocalWon);
        Assert.Equal(92, existing.ScorePercent);
        Assert.Equal(180, existing.TimeTakenSeconds);
        Assert.Equal(localTs, existing.SubmittedAt);
        Assert.Single(logs);
    }

    [Fact]
    public async Task Missing_server_progress_throws_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ResolveConflictCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            [],
            [],
            [ModuleRow(tenantId, Guid.NewGuid())],
            [],
            []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new ResolveConflictCommand(
                    tenantId,
                    studentId,
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    ModulePayload(Guid.NewGuid())),
                CancellationToken.None));
    }

    [Fact]
    public async Task Student_cannot_resolve_another_students_conflict()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ResolveConflictCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            [],
            [],
            [],
            [],
            [],
            currentUserId: Guid.NewGuid());

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new ResolveConflictCommand(
                    tenantId,
                    studentId,
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    ModulePayload(Guid.NewGuid())),
                CancellationToken.None));
    }

    private static ResolveConflictCommandHandler CreateHandler(
        Guid tenantId,
        Guid studentId,
        List<StudentProgress> progresses,
        List<AssessmentAttempt> attempts,
        List<Module> modules,
        List<SyncConflictLog> logs,
        List<ModuleCompletedNotification> published,
        List<Assessment>? assessments = null,
        Guid? currentUserId = null)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.StudentProgresses).Returns(MockSet(progresses).Object);
        db.Setup(x => x.AssessmentAttempts).Returns(MockSet(attempts).Object);
        db.Setup(x => x.Modules).Returns(MockSet(modules).Object);
        db.Setup(x => x.Assessments).Returns(MockSet(assessments ?? []).Object);
        db.Setup(x => x.SyncConflictLogs).Returns(MockSet(logs).Object);

        Mock<IPublisher> publisher = new();
        publisher
            .Setup(p => p.Publish(It.IsAny<ModuleCompletedNotification>(), It.IsAny<CancellationToken>()))
            .Callback((object notification, CancellationToken _) =>
            {
                if (notification is ModuleCompletedNotification evt)
                    published.Add(evt);
            })
            .Returns(Task.CompletedTask);

        return new ResolveConflictCommandHandler(
            db.Object,
            CurrentUser(tenantId, UserRole.Student, currentUserId ?? studentId).Object,
            publisher.Object,
            NullLogger<ResolveConflictCommandHandler>.Instance);
    }
}
