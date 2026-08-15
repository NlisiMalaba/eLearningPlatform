using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.AdaptiveLearning.Notifications;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class UpdateLearningProfileCommandHandlerTests
{
    [Fact]
    public async Task Persists_profile_to_cache_when_assessment_exists()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid assessmentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();

        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        List<Assessment> assessments = new()
        {
            new Assessment
            {
                Id = assessmentId,
                TenantId = tenantId,
                ModuleId = moduleId,
                Title = "A",
                PassingScorePercent = 60,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        db.Setup(x => x.Assessments).Returns(assessments.AsQueryable().BuildMockDbSet().Object);

        Mock<ICacheService> cache = new();
        cache.Setup(c => c.GetAsync<StudentLearningProfileDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudentLearningProfileDto?)null);
        cache.Setup(
                c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateLearningProfileCommandHandler(
            db.Object,
            cache.Object,
            NullLogger<UpdateLearningProfileCommandHandler>.Instance);

        await handler.Handle(
            new AssessmentSubmittedNotification(studentId, assessmentId, Guid.NewGuid(), tenantId, 82),
            CancellationToken.None);

        cache.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Module_completion_updates_profile_from_latest_module_attempt()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid assessmentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        DateTime submitted = DateTime.UtcNow.AddHours(-1);

        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        List<Assessment> assessments = new()
        {
            new Assessment
            {
                Id = assessmentId,
                TenantId = tenantId,
                ModuleId = moduleId,
                Title = "A",
                PassingScorePercent = 60,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        db.Setup(x => x.Assessments).Returns(assessments.AsQueryable().BuildMockDbSet().Object);

        List<AssessmentAttempt> attempts = new()
        {
            new AssessmentAttempt
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AssessmentId = assessmentId,
                StudentId = studentId,
                ScorePercent = 88,
                TimeTakenSeconds = 120,
                StartedAt = submitted.AddMinutes(-15),
                SubmittedAt = submitted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        db.Setup(x => x.AssessmentAttempts).Returns(attempts.AsQueryable().BuildMockDbSet().Object);

        Mock<ICacheService> cache = new();
        cache.Setup(c => c.GetAsync<StudentLearningProfileDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudentLearningProfileDto?)null);
        cache.Setup(
                c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateLearningProfileCommandHandler(
            db.Object,
            cache.Object,
            NullLogger<UpdateLearningProfileCommandHandler>.Instance);

        await handler.Handle(
            new ModuleCompletedNotification(studentId, moduleId, tenantId),
            CancellationToken.None);

        cache.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
