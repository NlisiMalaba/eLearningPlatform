using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Sync.Commands.ProcessOfflineQueue;
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

public sealed class ProcessOfflineQueueCommandHandlerTests
{
    [Fact]
    public async Task Applies_pending_module_progress_when_no_server_record_exists()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        DateTime localTs = DateTime.UtcNow;
        List<StudentProgress> progresses = [];
        List<ModuleCompletedNotification> published = [];
        List<OfflineSyncQueue> queues =
        [
            QueueItem(tenantId, studentId, localTs, ModulePayload(moduleId, completedAt: localTs)),
        ];
        ProcessOfflineQueueCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            queues,
            progresses,
            [],
            [ModuleRow(tenantId, moduleId)],
            [],
            published);

        ProcessOfflineQueueResultDto result = await handler.Handle(
            new ProcessOfflineQueueCommand(tenantId, studentId, []),
            CancellationToken.None);

        StudentProgress row = Assert.Single(progresses);
        Assert.Equal(studentId, row.StudentId);
        Assert.Equal(moduleId, row.ModuleId);
        Assert.True(row.IsCompleted);
        Assert.Equal(120, row.TimeOnTaskSeconds);
        Assert.Equal(SyncStatus.Synced, queues[0].Status);
        Assert.Equal(0, result.AcceptedCount);
        Assert.Equal(1, result.SyncedCount);
        Assert.Equal(0, result.ConflictedCount);
        Assert.Single(published);
    }

    [Fact]
    public async Task Applies_pending_assessment_attempt_when_no_server_record_exists()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid assessmentId = Guid.NewGuid();
        DateTime submittedAt = DateTime.UtcNow;
        List<AssessmentAttempt> attempts = [];
        List<AssessmentSubmittedNotification> submitted = [];
        List<OfflineSyncQueue> queues =
        [
            QueueItem(tenantId, studentId, submittedAt, AttemptPayload(assessmentId, 88, 240, submittedAt)),
        ];
        ProcessOfflineQueueCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            queues,
            [],
            attempts,
            [],
            [AssessmentRow(tenantId, assessmentId, Guid.NewGuid())],
            moduleCompleted: [],
            assessmentSubmitted: submitted);

        ProcessOfflineQueueResultDto result = await handler.Handle(
            new ProcessOfflineQueueCommand(tenantId, studentId, []),
            CancellationToken.None);

        AssessmentAttempt row = Assert.Single(attempts);
        Assert.Equal(88, row.ScorePercent);
        Assert.Equal(240, row.TimeTakenSeconds);
        Assert.Equal(submittedAt, row.SubmittedAt);
        Assert.Equal(SyncStatus.Synced, queues[0].Status);
        Assert.Equal(1, result.SyncedCount);
        AssessmentSubmittedNotification evt = Assert.Single(submitted);
        Assert.Equal(88, evt.ScorePercent);
    }

    [Fact]
    public async Task Dispatches_resolve_conflict_when_server_progress_exists()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        DateTime localTs = DateTime.UtcNow;
        StudentProgress existing = Progress(tenantId, studentId, moduleId, localTs.AddHours(-2));
        List<ResolveConflictCommand> sent = [];
        List<OfflineSyncQueue> queues =
        [
            QueueItem(tenantId, studentId, localTs, ModulePayload(moduleId)),
        ];
        ProcessOfflineQueueCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            queues,
            [existing],
            [],
            [ModuleRow(tenantId, moduleId)],
            [],
            [],
            resolveSent: sent);

        ProcessOfflineQueueResultDto result = await handler.Handle(
            new ProcessOfflineQueueCommand(tenantId, studentId, []),
            CancellationToken.None);

        ResolveConflictCommand command = Assert.Single(sent);
        Assert.Equal(queues[0].Id, command.QueueItemId);
        Assert.Equal(localTs, command.LocalTimestamp);
        Assert.Equal(SyncStatus.Conflicted, queues[0].Status);
        Assert.Equal(1, result.ConflictedCount);
        Assert.Equal(10, existing.TimeOnTaskSeconds);
    }

    [Fact]
    public async Task Enqueues_uploaded_items_with_client_id()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        DateTime localTs = DateTime.UtcNow;
        List<OfflineSyncQueue> queues = [];
        ProcessOfflineQueueCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            queues,
            [],
            [],
            [ModuleRow(tenantId, moduleId)],
            [],
            []);

        ProcessOfflineQueueResultDto result = await handler.Handle(
            new ProcessOfflineQueueCommand(
                tenantId,
                studentId,
                [new OfflineSyncItemDto(clientId, localTs, ModulePayload(moduleId, completedAt: localTs))]),
            CancellationToken.None);

        OfflineSyncQueue queued = Assert.Single(queues);
        Assert.Equal(clientId, queued.Id);
        Assert.Equal(1, result.AcceptedCount);
        Assert.Equal(SyncStatus.Synced, queued.Status);
    }

    [Fact]
    public async Task Skips_duplicate_client_id()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        DateTime localTs = DateTime.UtcNow;
        OfflineSyncQueue existing = QueueItem(
            tenantId,
            studentId,
            localTs,
            ModulePayload(moduleId),
            SyncStatus.Synced,
            clientId);
        List<OfflineSyncQueue> queues = [existing];
        ProcessOfflineQueueCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            queues,
            [],
            [],
            [ModuleRow(tenantId, moduleId)],
            [],
            []);

        ProcessOfflineQueueResultDto result = await handler.Handle(
            new ProcessOfflineQueueCommand(
                tenantId,
                studentId,
                [new OfflineSyncItemDto(clientId, localTs, ModulePayload(moduleId))]),
            CancellationToken.None);

        Assert.Equal(0, result.AcceptedCount);
        Assert.Single(queues);
        Assert.Equal(SyncStatus.Synced, existing.Status);
    }

    [Fact]
    public async Task Student_cannot_upload_another_students_queue()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ProcessOfflineQueueCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            [],
            [],
            [],
            [],
            [],
            [],
            currentUserId: Guid.NewGuid());

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new ProcessOfflineQueueCommand(tenantId, studentId, []),
                CancellationToken.None));
    }

    [Fact]
    public async Task Missing_student_throws_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ProcessOfflineQueueCommandHandler handler = CreateHandler(
            tenantId,
            studentId,
            [],
            [],
            [],
            [],
            [],
            [],
            users: []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new ProcessOfflineQueueCommand(tenantId, studentId, []),
                CancellationToken.None));
    }

    private static ProcessOfflineQueueCommandHandler CreateHandler(
        Guid tenantId,
        Guid studentId,
        List<OfflineSyncQueue> queues,
        List<StudentProgress> progresses,
        List<AssessmentAttempt> attempts,
        List<Module> modules,
        List<Assessment> assessments,
        List<ModuleCompletedNotification> moduleCompleted,
        List<AssessmentSubmittedNotification>? assessmentSubmitted = null,
        List<ResolveConflictCommand>? resolveSent = null,
        Guid? currentUserId = null,
        List<ApplicationUser>? users = null)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(MockSet(users ?? [Student(tenantId, studentId)]).Object);
        db.Setup(x => x.OfflineSyncQueues).Returns(MockSet(queues).Object);
        db.Setup(x => x.StudentProgresses).Returns(MockSet(progresses).Object);
        db.Setup(x => x.AssessmentAttempts).Returns(MockSet(attempts).Object);
        db.Setup(x => x.Modules).Returns(MockSet(modules).Object);
        db.Setup(x => x.Assessments).Returns(MockSet(assessments).Object);

        Mock<IPublisher> publisher = new();
        publisher
            .Setup(p => p.Publish(It.IsAny<ModuleCompletedNotification>(), It.IsAny<CancellationToken>()))
            .Callback((object notification, CancellationToken _) =>
            {
                if (notification is ModuleCompletedNotification evt)
                    moduleCompleted.Add(evt);
            })
            .Returns(Task.CompletedTask);
        publisher
            .Setup(p => p.Publish(It.IsAny<AssessmentSubmittedNotification>(), It.IsAny<CancellationToken>()))
            .Callback((object notification, CancellationToken _) =>
            {
                if (notification is AssessmentSubmittedNotification evt)
                    assessmentSubmitted?.Add(evt);
            })
            .Returns(Task.CompletedTask);

        Mock<IMediator> mediator = new();
        mediator
            .Setup(m => m.Send(It.IsAny<ResolveConflictCommand>(), It.IsAny<CancellationToken>()))
            .Returns((ResolveConflictCommand command, CancellationToken _) =>
            {
                resolveSent?.Add(command);
                return Task.FromResult(
                    new ResolveConflictResultDto(
                        true,
                        command.LocalTimestamp,
                        command.LocalTimestamp,
                        command.LocalTimestamp));
            });

        return new ProcessOfflineQueueCommandHandler(
            db.Object,
            CurrentUser(tenantId, UserRole.Student, currentUserId ?? studentId).Object,
            publisher.Object,
            mediator.Object,
            NullLogger<ProcessOfflineQueueCommandHandler>.Instance);
    }
}
