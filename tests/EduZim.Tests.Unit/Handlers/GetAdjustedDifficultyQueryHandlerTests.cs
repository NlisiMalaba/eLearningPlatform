using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.AdaptiveLearning.Queries.GetAdjustedDifficulty;
using EduZim.Application.AdaptiveLearning.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetAdjustedDifficultyQueryHandlerTests
{
    [Fact]
    public async Task Uses_cached_profile_and_adjusts_below_seventy()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();

        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        List<Module> modules = new()
        {
            new Module
            {
                Id = moduleId,
                TenantId = tenantId,
                Title = "M",
                Grade = GradeLevel.Grade1,
                Subject = "S",
                SequenceOrder = 1,
                IsRequired = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        db.Setup(x => x.Modules).Returns(modules.AsQueryable().BuildMockDbSet().Object);

        db.Setup(x => x.ParentStudentLinks).Returns(new List<ParentStudentLink>().AsQueryable().BuildMockDbSet().Object);

        Mock<ICacheService> cache = new();
        StudentLearningProfileDto profile = new(
            studentId,
            DateTime.UtcNow,
            Guid.NewGuid(),
            moduleId,
            50,
            4);
        cache.Setup(c => c.GetAsync<StudentLearningProfileDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Teacher);
        user.Setup(u => u.TenantId).Returns(tenantId);

        var handler = new GetAdjustedDifficultyQueryHandler(
            db.Object,
            cache.Object,
            user.Object,
            NullLogger<GetAdjustedDifficultyQueryHandler>.Instance);

        AdjustedDifficultyDto result = await handler.Handle(
            new GetAdjustedDifficultyQuery(tenantId, studentId, moduleId),
            CancellationToken.None);

        int expected = AdaptiveDifficulty.ComputeAdjustedTier(4, 50);
        Assert.Equal(expected, result.DifficultyTier);
        Assert.Equal(50, result.LastScorePercent);
    }
}
