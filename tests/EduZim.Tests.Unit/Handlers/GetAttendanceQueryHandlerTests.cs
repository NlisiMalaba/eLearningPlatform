using EduZim.Application.Common.Interfaces;
using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Application.LiveClassrooms.Queries.GetAttendance;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using static EduZim.Tests.Unit.Handlers.LiveClassroomTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetAttendanceQueryHandlerTests
{
    [Fact]
    public async Task Teacher_receives_attendance_rows()
    {
        Guid tenantId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime join = DateTime.UtcNow.AddMinutes(-15);
        GetAttendanceQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [Session(tenantId, sessionId, Guid.NewGuid(), teacherId, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow)],
            [Attendance(tenantId, sessionId, studentId, join, 900)]);

        SessionAttendanceDto dto = await handler.Handle(
            new GetAttendanceQuery(tenantId, sessionId),
            CancellationToken.None);

        AttendanceEntryDto entry = Assert.Single(dto.Entries);
        Assert.Equal(studentId, entry.StudentUserId);
        Assert.Equal(900, entry.DurationSeconds);
    }

    [Fact]
    public async Task Student_cannot_view_attendance()
    {
        Guid tenantId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        GetAttendanceQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            [Session(tenantId, sessionId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow)],
            []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetAttendanceQuery(tenantId, sessionId), CancellationToken.None));
    }

    private static AttendanceRecord Attendance(
        Guid tenantId,
        Guid sessionId,
        Guid studentId,
        DateTime join,
        int duration)
    {
        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClassroomSessionId = sessionId,
            StudentUserId = studentId,
            JoinTimeUtc = join,
            DurationSeconds = duration,
            CreatedAt = join,
            UpdatedAt = join,
        };
    }

    private static GetAttendanceQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<ClassroomSession> sessions,
        List<AttendanceRecord> rows)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.ClassroomSessions).Returns(sessions.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.AttendanceRecords).Returns(rows.AsQueryable().BuildMockDbSet().Object);

        return new GetAttendanceQueryHandler(
            db.Object,
            CurrentUser(tenantId, role, userId).Object,
            NullLogger<GetAttendanceQueryHandler>.Instance);
    }
}
