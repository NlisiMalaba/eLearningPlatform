using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using EduZim.Application.Gamification.Queries.GetStudentBadges;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetStudentBadgesQueryHandlerTests
{
    [Fact]
    public async Task Returns_badges_for_the_requested_student_only()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid otherStudent = Guid.NewGuid();
        DateTime earned = DateTime.UtcNow.AddDays(-1);
        GetStudentBadgesQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            Guid.NewGuid(),
            [
                Badge(tenantId, studentId, BadgeType.FirstModule, earned),
                Badge(tenantId, otherStudent, BadgeType.GradeCompletion, earned),
            ]);

        StudentBadgesDto dto = await handler.Handle(
            new GetStudentBadgesQuery(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(studentId, dto.StudentId);
        Assert.Single(dto.Badges);
        Assert.Equal(BadgeType.FirstModule, dto.Badges[0].Type);
    }

    [Fact]
    public async Task Returns_empty_list_when_student_has_no_badges()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        GetStudentBadgesQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            Guid.NewGuid(),
            []);

        StudentBadgesDto dto = await handler.Handle(
            new GetStudentBadgesQuery(tenantId, studentId),
            CancellationToken.None);

        Assert.Empty(dto.Badges);
    }

    [Fact]
    public async Task Student_cannot_view_another_students_badges()
    {
        Guid tenantId = Guid.NewGuid();
        GetStudentBadgesQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetStudentBadgesQuery(tenantId, Guid.NewGuid()), CancellationToken.None));
    }

    private static Badge Badge(Guid tenantId, Guid studentId, BadgeType type, DateTime earnedAt)
    {
        return new Badge
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            Type = type,
            EarnedAt = earnedAt,
            CreatedAt = earnedAt,
            UpdatedAt = earnedAt,
        };
    }

    private static GetStudentBadgesQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<Badge> badges)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Badges).Returns(badges.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ParentStudentLinks)
            .Returns(new List<ParentStudentLink>().AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(userId);

        return new GetStudentBadgesQueryHandler(
            db.Object,
            user.Object,
            NullLogger<GetStudentBadgesQueryHandler>.Instance);
    }
}
