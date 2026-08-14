using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MockQueryable.Moq;
using Moq;
using static EduZim.Tests.Unit.Handlers.LiveClassroomTestData;

namespace EduZim.Tests.Unit.Services;

public sealed class ClassroomRealtimeServiceTests
{
    [Fact]
    public async Task Enrolled_student_can_join()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ClassroomRealtimeService service = CreateService(
            tenantId,
            UserRole.Student,
            studentId,
            [Session(tenantId, sessionId, classId, Guid.NewGuid(), DateTime.UtcNow.AddHours(1))],
            [Enrollment(tenantId, classId, studentId)],
            []);

        ClassroomSession session = await service.AuthorizeJoinAsync(sessionId, CancellationToken.None);

        Assert.Equal(sessionId, session.Id);
    }

    [Fact]
    public async Task Unenrolled_student_cannot_join()
    {
        Guid tenantId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        ClassroomRealtimeService service = CreateService(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            [Session(tenantId, sessionId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(1))],
            [],
            []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => service.AuthorizeJoinAsync(sessionId, CancellationToken.None));
    }

    [Fact]
    public async Task Ended_session_cannot_be_joined()
    {
        Guid tenantId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        ClassroomRealtimeService service = CreateService(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [Session(tenantId, sessionId, Guid.NewGuid(), teacherId, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow)],
            [],
            []);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.AuthorizeJoinAsync(sessionId, CancellationToken.None));
    }

    [Fact]
    public async Task Teacher_can_control_and_student_cannot()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ClassroomSession session = Session(tenantId, sessionId, classId, teacherId, DateTime.UtcNow);
        ClassroomRealtimeService teacher = CreateService(
            tenantId, UserRole.Teacher, teacherId, [session], [Enrollment(tenantId, classId, studentId)], []);
        ClassroomRealtimeService student = CreateService(
            tenantId, UserRole.Student, studentId, [session], [Enrollment(tenantId, classId, studentId)], []);

        ClassroomSession controlled = await teacher.AuthorizeControlAsync(sessionId, CancellationToken.None);
        Assert.Equal(sessionId, controlled.Id);
        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => student.AuthorizeControlAsync(sessionId, CancellationToken.None));
    }

    [Fact]
    public async Task Teacher_can_mute_enrolled_student()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ClassroomRealtimeService service = CreateService(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [Session(tenantId, sessionId, classId, teacherId, DateTime.UtcNow)],
            [Enrollment(tenantId, classId, studentId)],
            []);

        ClassroomSession session = await service.AuthorizeStudentMediaAsync(
            sessionId,
            studentId,
            CancellationToken.None);

        Assert.Equal(sessionId, session.Id);
    }

    [Fact]
    public async Task Present_content_requires_existing_item()
    {
        Guid tenantId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid contentId = Guid.NewGuid();
        ClassroomRealtimeService missing = CreateService(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [Session(tenantId, sessionId, Guid.NewGuid(), teacherId, DateTime.UtcNow)],
            [],
            []);
        ClassroomRealtimeService present = CreateService(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [Session(tenantId, sessionId, Guid.NewGuid(), teacherId, DateTime.UtcNow)],
            [],
            [Content(tenantId, contentId)]);

        await Assert.ThrowsAsync<NotFoundException>(
            () => missing.AuthorizePresentContentAsync(sessionId, contentId, CancellationToken.None));
        ClassroomSession session = await present.AuthorizePresentContentAsync(
            sessionId,
            contentId,
            CancellationToken.None);
        Assert.Equal(sessionId, session.Id);
    }

    private static ContentItem Content(Guid tenantId, Guid contentId)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ContentItem
        {
            Id = contentId,
            TenantId = tenantId,
            Title = "Lesson slides",
            Type = ContentType.Pdf,
            StorageKey = $"{tenantId}/content/{contentId}/slides",
            FileSizeBytes = 10,
            Language = "en",
            Status = ContentStatus.Published,
            UploadedByUserId = Guid.NewGuid(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    private static ClassroomRealtimeService CreateService(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<ClassroomSession> sessions,
        List<ClassEnrollment> enrollments,
        List<ContentItem> content)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.ClassroomSessions).Returns(sessions.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ClassEnrollments).Returns(enrollments.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ContentItems).Returns(content.AsQueryable().BuildMockDbSet().Object);

        return new ClassroomRealtimeService(db.Object, CurrentUser(tenantId, role, userId).Object);
    }
}
