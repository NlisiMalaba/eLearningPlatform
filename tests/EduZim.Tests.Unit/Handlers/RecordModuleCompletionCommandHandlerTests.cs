using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.Notifications;
using EduZim.Domain.Entities;
using EduZim.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class RecordModuleCompletionCommandHandlerTests
{
    [Fact]
    public async Task Completing_module_marks_progress_and_unlocks_next()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        List<StudentProgress> captured = [];
        List<SubjectProgress> subjects = [];
        RecordModuleCompletionCommandHandler handler = CreateHandler(
            [
                ProgressTestData.Module(tenantId, first, "Math", 1),
                ProgressTestData.Module(tenantId, second, "Math", 2),
            ],
            progresses: [],
            captured,
            storedSubjects: [],
            subjects);

        await handler.Handle(new ModuleCompletedNotification(studentId, first, tenantId), CancellationToken.None);

        StudentProgress completed = Assert.Single(captured, p => p.ModuleId == first);
        Assert.True(completed.IsCompleted);
        Assert.True(completed.IsUnlocked);
        Assert.NotNull(completed.CompletedAt);

        StudentProgress unlocked = Assert.Single(captured, p => p.ModuleId == second);
        Assert.False(unlocked.IsCompleted);
        Assert.True(unlocked.IsUnlocked);

        SubjectProgress stored = Assert.Single(subjects);
        Assert.Equal("Math", stored.Subject);
        Assert.Equal(50, stored.ProgressPercent);
    }

    [Fact]
    public async Task Completing_last_module_stores_100_percent_without_next_unlock()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid only = Guid.NewGuid();
        List<StudentProgress> captured = [];
        List<SubjectProgress> subjects = [];
        RecordModuleCompletionCommandHandler handler = CreateHandler(
            [ProgressTestData.Module(tenantId, only, "English", 1)],
            progresses: [],
            captured,
            storedSubjects: [],
            subjects);

        await handler.Handle(new ModuleCompletedNotification(studentId, only, tenantId), CancellationToken.None);

        Assert.Single(captured);
        Assert.Equal(100, Assert.Single(subjects).ProgressPercent);
    }

    [Fact]
    public async Task Existing_progress_is_updated_idempotently()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        StudentProgress existing = ProgressTestData.Progress(tenantId, studentId, first, completed: false, unlocked: true);
        List<StudentProgress> captured = [];
        RecordModuleCompletionCommandHandler handler = CreateHandler(
            [
                ProgressTestData.Module(tenantId, first, "Math", 1),
                ProgressTestData.Module(tenantId, second, "Math", 2),
            ],
            progresses: [existing],
            captured,
            storedSubjects: [],
            capturedSubjects: []);

        await handler.Handle(new ModuleCompletedNotification(studentId, first, tenantId), CancellationToken.None);

        Assert.True(existing.IsCompleted);
        Assert.NotNull(existing.CompletedAt);
        Assert.Single(captured, p => p.ModuleId == second && p.IsUnlocked);
    }

    [Fact]
    public async Task Missing_module_does_not_write_progress()
    {
        Guid tenantId = Guid.NewGuid();
        List<StudentProgress> captured = [];
        RecordModuleCompletionCommandHandler handler = CreateHandler(
            modules: [],
            progresses: [],
            captured,
            storedSubjects: [],
            capturedSubjects: []);

        await handler.Handle(
            new ModuleCompletedNotification(Guid.NewGuid(), Guid.NewGuid(), tenantId),
            CancellationToken.None);

        Assert.Empty(captured);
    }

    [Fact]
    public async Task Rounds_subject_percent_to_nearest_integer()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid m1 = Guid.NewGuid();
        List<SubjectProgress> subjects = [];
        RecordModuleCompletionCommandHandler handler = CreateHandler(
            [
                ProgressTestData.Module(tenantId, m1, "Science", 1),
                ProgressTestData.Module(tenantId, Guid.NewGuid(), "Science", 2),
                ProgressTestData.Module(tenantId, Guid.NewGuid(), "Science", 3),
            ],
            progresses: [],
            capturedProgress: [],
            storedSubjects: [],
            subjects);

        await handler.Handle(new ModuleCompletedNotification(studentId, m1, tenantId), CancellationToken.None);

        Assert.Equal(33, Assert.Single(subjects).ProgressPercent);
    }

    private static RecordModuleCompletionCommandHandler CreateHandler(
        List<Module> modules,
        List<StudentProgress> progresses,
        List<StudentProgress> capturedProgress,
        List<SubjectProgress> storedSubjects,
        List<SubjectProgress> capturedSubjects)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Modules).Returns(modules.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<StudentProgress>> progressSet = progresses.AsQueryable().BuildMockDbSet();
        progressSet.Setup(s => s.AddAsync(It.IsAny<StudentProgress>(), It.IsAny<CancellationToken>()))
            .Callback<StudentProgress, CancellationToken>((entity, _) =>
            {
                progresses.Add(entity);
                capturedProgress.Add(entity);
            })
            .Returns(ValueTask.FromResult((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<StudentProgress>)null!));
        db.Setup(x => x.StudentProgresses).Returns(progressSet.Object);

        Mock<DbSet<SubjectProgress>> subjectSet = storedSubjects.AsQueryable().BuildMockDbSet();
        subjectSet.Setup(s => s.AddAsync(It.IsAny<SubjectProgress>(), It.IsAny<CancellationToken>()))
            .Callback<SubjectProgress, CancellationToken>((entity, _) => capturedSubjects.Add(entity))
            .Returns(ValueTask.FromResult((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<SubjectProgress>)null!));
        db.Setup(x => x.SubjectProgresses).Returns(subjectSet.Object);

        return new RecordModuleCompletionCommandHandler(
            db.Object,
            NullLogger<RecordModuleCompletionCommandHandler>.Instance);
    }
}
