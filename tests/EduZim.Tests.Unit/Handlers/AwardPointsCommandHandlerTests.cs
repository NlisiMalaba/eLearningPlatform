using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.Notifications;
using EduZim.Application.Gamification.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class AwardPointsCommandHandlerTests
{
    [Fact]
    public async Task Module_completion_creates_record_with_base_points()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        List<StudentPoints> captured = new();
        AwardPointsCommandHandler handler = CreateHandler(new List<StudentPoints>(), captured);

        await handler.Handle(
            new ModuleCompletedNotification(studentId, Guid.NewGuid(), tenantId),
            CancellationToken.None);

        Assert.Single(captured);
        Assert.Equal(PointsAwardRules.ModuleCompletionBasePoints, captured[0].TotalPoints);
        Assert.Equal(studentId, captured[0].StudentId);
        Assert.Equal(tenantId, captured[0].TenantId);
    }

    [Fact]
    public async Task Assessment_below_threshold_adds_base_points()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        StudentPoints existing = ExistingPoints(tenantId, studentId, 5);
        AwardPointsCommandHandler handler = CreateHandler(new List<StudentPoints> { existing }, new List<StudentPoints>());

        await handler.Handle(
            new AssessmentSubmittedNotification(studentId, Guid.NewGuid(), Guid.NewGuid(), tenantId, 84),
            CancellationToken.None);

        Assert.Equal(5 + PointsAwardRules.AssessmentCompletionBasePoints, existing.TotalPoints);
    }

    [Fact]
    public async Task Assessment_at_or_above_85_includes_bonus_multiplier()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        StudentPoints existing = ExistingPoints(tenantId, studentId, 5);
        AwardPointsCommandHandler handler = CreateHandler(new List<StudentPoints> { existing }, new List<StudentPoints>());

        await handler.Handle(
            new AssessmentSubmittedNotification(studentId, Guid.NewGuid(), Guid.NewGuid(), tenantId, 85),
            CancellationToken.None);

        int expectedAward = PointsAwardRules.ForAssessment(85);
        Assert.Equal(5 + expectedAward, existing.TotalPoints);
        Assert.True(expectedAward > PointsAwardRules.AssessmentCompletionBasePoints);
    }

    private static StudentPoints ExistingPoints(Guid tenantId, Guid studentId, int total)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new StudentPoints
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            TotalPoints = total,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    private static AwardPointsCommandHandler CreateHandler(
        List<StudentPoints> existing,
        List<StudentPoints> capturedAdds)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Mock<DbSet<StudentPoints>> set = existing.AsQueryable().BuildMockDbSet();
        set.Setup(s => s.AddAsync(It.IsAny<StudentPoints>(), It.IsAny<CancellationToken>()))
            .Callback<StudentPoints, CancellationToken>((entity, _) => capturedAdds.Add(entity))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<StudentPoints>>(
                (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<StudentPoints>)null!));
        db.Setup(x => x.StudentPoints).Returns(set.Object);

        return new AwardPointsCommandHandler(db.Object, NullLogger<AwardPointsCommandHandler>.Instance);
    }
}
