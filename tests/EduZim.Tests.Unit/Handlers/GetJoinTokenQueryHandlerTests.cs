using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Application.LiveClassrooms.Queries.GetJoinToken;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using static EduZim.Tests.Unit.Handlers.LiveClassroomTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetJoinTokenQueryHandlerTests
{
    [Fact]
    public async Task Enrolled_student_receives_token_and_is_recorded()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        List<ClassroomParticipant> captured = [];
        Mock<IVideoService> video = new();
        video.Setup(v => v.GetJoinTokenAsync(It.IsAny<string>(), studentId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("join-token");
        GetJoinTokenQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [Session(tenantId, sessionId, classId, Guid.NewGuid(), DateTime.UtcNow.AddHours(1))],
            [Enrollment(tenantId, classId, studentId)],
            captured,
            video);

        JoinTokenDto dto = await handler.Handle(
            new GetJoinTokenQuery(tenantId, sessionId),
            CancellationToken.None);

        Assert.Equal("join-token", dto.Token);
        Assert.Equal(sessionId, dto.SessionId);
        ClassroomParticipant participant = Assert.Single(captured);
        Assert.Equal(studentId, participant.UserId);
    }

    [Fact]
    public async Task Unenrolled_student_is_forbidden()
    {
        Guid tenantId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        GetJoinTokenQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            [Session(tenantId, sessionId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(1))],
            [],
            [],
            new Mock<IVideoService>());

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetJoinTokenQuery(tenantId, sessionId), CancellationToken.None));
    }

    [Fact]
    public async Task Ended_session_cannot_be_joined()
    {
        Guid tenantId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        GetJoinTokenQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [Session(tenantId, sessionId, Guid.NewGuid(), teacherId, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow)],
            [],
            [],
            new Mock<IVideoService>());

        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(new GetJoinTokenQuery(tenantId, sessionId), CancellationToken.None));
    }

    private static GetJoinTokenQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<ClassroomSession> sessions,
        List<ClassEnrollment> enrollments,
        List<ClassroomParticipant> captured,
        Mock<IVideoService> video)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.ClassroomSessions).Returns(sessions.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ClassEnrollments).Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<ClassroomParticipant>> participants = new List<ClassroomParticipant>().AsQueryable()
            .BuildMockDbSet();
        participants.Setup(s => s.AddAsync(It.IsAny<ClassroomParticipant>(), It.IsAny<CancellationToken>()))
            .Callback<ClassroomParticipant, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ClassroomParticipant>>(
                (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ClassroomParticipant>)null!));
        db.Setup(x => x.ClassroomParticipants).Returns(participants.Object);

        return new GetJoinTokenQueryHandler(
            db.Object,
            CurrentUser(tenantId, role, userId).Object,
            video.Object,
            NullLogger<GetJoinTokenQueryHandler>.Instance);
    }
}
