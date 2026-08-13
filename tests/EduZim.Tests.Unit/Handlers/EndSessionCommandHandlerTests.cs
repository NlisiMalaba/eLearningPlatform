using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.Commands.EndSession;
using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using static EduZim.Tests.Unit.Handlers.LiveClassroomTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class EndSessionCommandHandlerTests
{
    [Fact]
    public async Task Creates_attendance_for_joined_students_and_stores_recording()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime joinedAt = DateTime.UtcNow.AddMinutes(-20);
        List<AttendanceRecord> captured = [];
        Mock<IVideoService> video = new();
        video.Setup(v => v.GetRecordingUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://rec.example/room");
        ClassroomSession session = Session(tenantId, sessionId, classId, teacherId, DateTime.UtcNow.AddHours(-1));
        EndSessionCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [session],
            [Enrollment(tenantId, classId, studentId)],
            [Participant(tenantId, sessionId, studentId, joinedAt), Participant(tenantId, sessionId, teacherId, joinedAt)],
            captured,
            video);

        SessionAttendanceDto dto = await handler.Handle(
            new EndSessionCommand(tenantId, sessionId),
            CancellationToken.None);

        AttendanceEntryDto entry = Assert.Single(dto.Entries);
        Assert.Equal(studentId, entry.StudentUserId);
        Assert.Equal(joinedAt, entry.JoinTimeUtc);
        Assert.True(entry.DurationSeconds >= 0);
        Assert.Equal("https://rec.example/room", session.RecordingUrl);
        Assert.NotNull(session.SessionEndTime);
        Assert.Single(captured);
    }

    [Fact]
    public async Task Already_ended_session_throws_conflict()
    {
        Guid tenantId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        EndSessionCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [Session(tenantId, sessionId, Guid.NewGuid(), teacherId, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow)],
            [],
            [],
            [],
            new Mock<IVideoService>());

        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(new EndSessionCommand(tenantId, sessionId), CancellationToken.None));
    }

    private static EndSessionCommandHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<ClassroomSession> sessions,
        List<ClassEnrollment> enrollments,
        List<ClassroomParticipant> participants,
        List<AttendanceRecord> captured,
        Mock<IVideoService> video)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.ClassroomSessions).Returns(sessions.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ClassEnrollments).Returns(enrollments.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ClassroomParticipants).Returns(participants.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<AttendanceRecord>> attendance = new List<AttendanceRecord>().AsQueryable().BuildMockDbSet();
        attendance.Setup(s => s.AddAsync(It.IsAny<AttendanceRecord>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceRecord, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AttendanceRecord>>(
                (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AttendanceRecord>)null!));
        db.Setup(x => x.AttendanceRecords).Returns(attendance.Object);

        return new EndSessionCommandHandler(
            db.Object,
            CurrentUser(tenantId, role, userId).Object,
            video.Object,
            NullLogger<EndSessionCommandHandler>.Instance);
    }
}
