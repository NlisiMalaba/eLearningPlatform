using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using EduZim.Application.Gamification.Queries.GetLeaderboard;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetLeaderboardQueryHandlerTests
{
    [Fact]
    public async Task Returns_only_students_from_requesting_tenant_ranked_by_points()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        Guid studentHigh = Guid.NewGuid();
        Guid studentLow = Guid.NewGuid();
        Guid outsider = Guid.NewGuid();

        List<StudentPoints> points =
        [
            Points(tenantA, studentLow, 10),
            Points(tenantA, studentHigh, 40),
            Points(tenantB, outsider, 999),
        ];

        GetLeaderboardQueryHandler handler = CreateHandler(tenantA, UserRole.Teacher, points);

        LeaderboardDto dto = await handler.Handle(new GetLeaderboardQuery(tenantA, Limit: 10), CancellationToken.None);

        Assert.Equal(2, dto.Entries.Count);
        Assert.All(dto.Entries, e => Assert.Equal(tenantA, e.TenantId));
        Assert.Equal(studentHigh, dto.Entries[0].StudentId);
        Assert.Equal(1, dto.Entries[0].Rank);
        Assert.Equal(studentLow, dto.Entries[1].StudentId);
        Assert.Equal(2, dto.Entries[1].Rank);
        Assert.DoesNotContain(dto.Entries, e => e.StudentId == outsider);
    }

    [Fact]
    public async Task Parent_cannot_view_leaderboard()
    {
        Guid tenantId = Guid.NewGuid();
        GetLeaderboardQueryHandler handler = CreateHandler(tenantId, UserRole.ParentGuardian, []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetLeaderboardQuery(tenantId), CancellationToken.None));
    }

    private static StudentPoints Points(Guid tenantId, Guid studentId, int total)
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

    private static GetLeaderboardQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        List<StudentPoints> points)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.StudentPoints).Returns(points.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(new List<ApplicationUser>().AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(Guid.NewGuid());

        return new GetLeaderboardQueryHandler(
            db.Object,
            user.Object,
            NullLogger<GetLeaderboardQueryHandler>.Instance);
    }
}
