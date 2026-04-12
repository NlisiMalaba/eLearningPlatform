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
}
