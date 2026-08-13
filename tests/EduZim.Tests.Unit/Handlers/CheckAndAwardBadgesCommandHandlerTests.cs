using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.Notifications;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class CheckAndAwardBadgesCommandHandlerTests
{
    [Fact]
    public async Task First_module_completion_awards_first_module_badge_and_queues_certificate()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        List<Badge> captured = new();
        RecordingJobs jobs = new();
        Mock<IPublisher> publisher = new();
        CheckAndAwardBadgesCommandHandler handler = CreateHandler(
            new List<Module>
            {
                Module(tenantId, moduleId, "Math", GradeLevel.Grade1),
                Module(tenantId, Guid.NewGuid(), "Math", GradeLevel.Grade1),
            },
            progresses: [],
            attempts: [],
            badges: [],
            captured,
            jobs,
            publisher);

        await handler.Handle(new ModuleCompletedNotification(studentId, moduleId, tenantId), CancellationToken.None);

        Assert.Single(captured);
        Assert.Equal(BadgeType.FirstModule, captured[0].Type);
        Assert.Single(jobs.Queued);
        publisher.Verify(
            p => p.Publish(It.Is<BadgeAwardedNotification>(n => n.BadgeType == BadgeType.FirstModule), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Does_not_duplicate_existing_first_module_badge()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        List<Badge> captured = new();
        RecordingJobs jobs = new();
        Mock<IPublisher> publisher = new();
        Badge existing = Badge(tenantId, studentId, BadgeType.FirstModule);
        CheckAndAwardBadgesCommandHandler handler = CreateHandler(
            new List<Module>
            {
                Module(tenantId, moduleId, "Math", GradeLevel.Grade1),
                Module(tenantId, Guid.NewGuid(), "Math", GradeLevel.Grade1),
            },
            progresses: [],
            attempts: [],
            badges: [existing],
            captured,
            jobs,
            publisher);

        await handler.Handle(new ModuleCompletedNotification(studentId, moduleId, tenantId), CancellationToken.None);

        Assert.Empty(captured);
        Assert.Empty(jobs.Queued);
        publisher.Verify(
            p => p.Publish(It.IsAny<BadgeAwardedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Five_consecutive_activity_days_awards_streak_badge()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime today = DateTime.UtcNow.Date;
        List<StudentProgress> progresses = Enumerable.Range(1, 4)
            .Select(i => Progress(tenantId, studentId, Guid.NewGuid(), today.AddDays(-i)))
            .ToList();
        List<Badge> captured = new();
        RecordingJobs jobs = new();
        Mock<IPublisher> publisher = new();
        CheckAndAwardBadgesCommandHandler handler = CreateHandler(
            modules: [],
            progresses,
            attempts: [],
            badges: [],
            captured,
            jobs,
            publisher);

        await handler.Handle(
            new AssessmentSubmittedNotification(studentId, Guid.NewGuid(), Guid.NewGuid(), tenantId, 70),
            CancellationToken.None);

        Assert.Contains(captured, b => b.Type == BadgeType.FiveConsecutiveDays);
        Assert.NotEmpty(jobs.Queued);
    }

    [Fact]
    public async Task Completing_all_modules_in_a_subject_awards_subject_mastery()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid m1 = Guid.NewGuid();
        Guid m2 = Guid.NewGuid();
        List<Badge> captured = new();
        RecordingJobs jobs = new();
        Mock<IPublisher> publisher = new();
        CheckAndAwardBadgesCommandHandler handler = CreateHandler(
            new List<Module>
            {
                Module(tenantId, m1, "Science", GradeLevel.Grade2),
                Module(tenantId, m2, "Science", GradeLevel.Grade2),
            },
            new List<StudentProgress> { Progress(tenantId, studentId, m1, DateTime.UtcNow) },
            attempts: [],
            badges: [],
            captured,
            jobs,
            publisher);

        await handler.Handle(new ModuleCompletedNotification(studentId, m2, tenantId), CancellationToken.None);

        Assert.Contains(captured, b => b.Type == BadgeType.SubjectMastery);
        Assert.Contains(captured, b => b.Type == BadgeType.GradeCompletion);
        Assert.Equal(captured.Count, jobs.Queued.Count);
    }

    private static Module Module(Guid tenantId, Guid id, string subject, GradeLevel grade)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new Module
        {
            Id = id,
            TenantId = tenantId,
            Title = subject,
            Subject = subject,
            Grade = grade,
            SequenceOrder = 1,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    private static StudentProgress Progress(Guid tenantId, Guid studentId, Guid moduleId, DateTime completedAt)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new StudentProgress
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            ModuleId = moduleId,
            IsCompleted = true,
            CompletedAt = completedAt,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    private static Badge Badge(Guid tenantId, Guid studentId, BadgeType type)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new Badge
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            Type = type,
            EarnedAt = utcNow,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    private static CheckAndAwardBadgesCommandHandler CreateHandler(
        List<Module> modules,
        List<StudentProgress> progresses,
        List<AssessmentAttempt> attempts,
        List<Badge> badges,
        List<Badge> capturedAdds,
        RecordingJobs jobs,
        Mock<IPublisher> publisher)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Modules).Returns(modules.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.StudentProgresses).Returns(progresses.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.AssessmentAttempts).Returns(attempts.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<Badge>> badgeSet = badges.AsQueryable().BuildMockDbSet();
        badgeSet.Setup(s => s.AddAsync(It.IsAny<Badge>(), It.IsAny<CancellationToken>()))
            .Callback<Badge, CancellationToken>((entity, _) => capturedAdds.Add(entity))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Badge>>(
                (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Badge>)null!));
        db.Setup(x => x.Badges).Returns(badgeSet.Object);

        publisher.Setup(p => p.Publish(It.IsAny<BadgeAwardedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new CheckAndAwardBadgesCommandHandler(
            db.Object,
            publisher.Object,
            jobs,
            NullLogger<CheckAndAwardBadgesCommandHandler>.Instance);
    }

    private sealed class RecordingJobs : IGamificationBackgroundJobs
    {
        public List<(Guid TenantId, Guid StudentId, Guid BadgeId)> Queued { get; } = new();

        public string? EnqueueCertificateGeneration(Guid tenantId, Guid studentId, Guid badgeId)
        {
            Queued.Add((tenantId, studentId, badgeId));
            return "job-id";
        }
    }
}
