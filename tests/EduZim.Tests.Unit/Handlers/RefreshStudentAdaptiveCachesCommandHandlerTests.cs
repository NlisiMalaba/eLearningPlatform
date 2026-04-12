using EduZim.Application.AdaptiveLearning.Commands.RefreshStudentAdaptiveCaches;
using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class RefreshStudentAdaptiveCachesCommandHandlerTests
{
    [Fact]
    public async Task Removes_weekly_cache_after_refresh()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid assessmentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();

        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        DateTime submitted = DateTime.UtcNow.AddHours(-1);
        List<AssessmentAttempt> attempts = new()
        {
            new AssessmentAttempt
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AssessmentId = assessmentId,
                StudentId = studentId,
                ScorePercent = 72,
                TimeTakenSeconds = 60,
                StartedAt = submitted.AddMinutes(-10),
                SubmittedAt = submitted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        db.Setup(x => x.AssessmentAttempts).Returns(attempts.AsQueryable().BuildMockDbSet().Object);

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

        db.Setup(x => x.ParentStudentLinks).Returns(new List<ParentStudentLink>().AsQueryable().BuildMockDbSet().Object);

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

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Teacher);
        user.Setup(u => u.TenantId).Returns(tenantId);

        RefreshStudentAdaptiveCachesCommandHandler handler = new(
            db.Object,
            cache.Object,
            user.Object,
            NullLogger<RefreshStudentAdaptiveCachesCommandHandler>.Instance);

        await handler.Handle(new RefreshStudentAdaptiveCachesCommand(tenantId, studentId), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
