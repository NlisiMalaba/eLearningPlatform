using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.Queries.GetRecordingUrl;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using static EduZim.Tests.Unit.Handlers.LiveClassroomTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetRecordingUrlQueryHandlerTests
{
    [Fact]
    public async Task Returns_url_within_thirty_days()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime ended = DateTime.UtcNow.AddDays(-10);
        GetRecordingUrlQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [Session(tenantId, sessionId, classId, Guid.NewGuid(), ended.AddHours(-1), ended, "https://rec.example/a")],
            [Enrollment(tenantId, classId, studentId)]);

        string? url = await handler.Handle(new GetRecordingUrlQuery(tenantId, sessionId), CancellationToken.None);

        Assert.Equal("https://rec.example/a", url);
    }

    [Fact]
    public async Task Returns_null_after_thirty_days()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime ended = DateTime.UtcNow.AddDays(-31);
        GetRecordingUrlQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [Session(tenantId, sessionId, classId, Guid.NewGuid(), ended.AddHours(-1), ended, "https://rec.example/a")],
            [Enrollment(tenantId, classId, studentId)]);

        string? url = await handler.Handle(new GetRecordingUrlQuery(tenantId, sessionId), CancellationToken.None);

        Assert.Null(url);
    }

    [Fact]
    public async Task Unended_session_throws_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        GetRecordingUrlQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [Session(tenantId, sessionId, Guid.NewGuid(), teacherId, DateTime.UtcNow.AddHours(1))],
            []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetRecordingUrlQuery(tenantId, sessionId), CancellationToken.None));
    }

    private static GetRecordingUrlQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<ClassroomSession> sessions,
        List<ClassEnrollment> enrollments)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.ClassroomSessions).Returns(sessions.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ClassEnrollments).Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

        return new GetRecordingUrlQueryHandler(
            db.Object,
            CurrentUser(tenantId, role, userId).Object,
            NullLogger<GetRecordingUrlQueryHandler>.Instance);
    }
}
