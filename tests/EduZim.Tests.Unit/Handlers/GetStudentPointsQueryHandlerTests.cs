using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using EduZim.Application.Gamification.Queries.GetStudentPoints;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetStudentPointsQueryHandlerTests
{
    [Fact]
    public async Task Returns_stored_total_for_teacher()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        GetStudentPointsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            Guid.NewGuid(),
            [Points(tenantId, studentId, 42)]);

        StudentPointsDto dto = await handler.Handle(
            new GetStudentPointsQuery(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(studentId, dto.StudentId);
        Assert.Equal(42, dto.TotalPoints);
    }

    [Fact]
    public async Task Returns_zero_when_student_has_no_points_record()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        GetStudentPointsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            Guid.NewGuid(),
            []);

        StudentPointsDto dto = await handler.Handle(
            new GetStudentPointsQuery(tenantId, studentId),
            CancellationToken.None);

        Assert.Equal(0, dto.TotalPoints);
    }

    [Fact]
    public async Task Student_cannot_view_another_students_points()
    {
        Guid tenantId = Guid.NewGuid();
        GetStudentPointsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetStudentPointsQuery(tenantId, Guid.NewGuid()), CancellationToken.None));
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

    private static GetStudentPointsQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<StudentPoints> points)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.StudentPoints).Returns(points.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ParentStudentLinks)
            .Returns(new List<ParentStudentLink>().AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(userId);

        return new GetStudentPointsQueryHandler(
            db.Object,
            user.Object,
            NullLogger<GetStudentPointsQueryHandler>.Instance);
    }
}
