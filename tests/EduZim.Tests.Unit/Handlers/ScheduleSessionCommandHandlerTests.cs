using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.Commands.ScheduleSession;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using static EduZim.Tests.Unit.Handlers.LiveClassroomTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ScheduleSessionCommandHandlerTests
{
    [Fact]
    public async Task Teacher_creates_session_and_publishes_scheduled_event()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        List<ClassroomSession> captured = [];
        List<ClassroomScheduledNotification> published = [];
        DateTime start = DateTime.UtcNow.AddHours(25);
        ScheduleSessionCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            teacherId,
            [Class(tenantId, classId)],
            captured,
            published);

        Guid sessionId = await handler.Handle(
            new ScheduleSessionCommand(tenantId, classId, start, 45),
            CancellationToken.None);

        ClassroomSession row = Assert.Single(captured);
        Assert.Equal(sessionId, row.Id);
        Assert.Equal(teacherId, row.TeacherUserId);
        Assert.Equal(start, row.StartAtUtc);
        Assert.Equal(start.AddMinutes(45), row.PlannedEndAtUtc);
        ClassroomScheduledNotification evt = Assert.Single(published);
        Assert.Equal(sessionId, evt.SessionId);
        Assert.Equal(tenantId, evt.TenantId);
    }

    [Fact]
    public async Task Missing_class_throws_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        ScheduleSessionCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            Guid.NewGuid(),
            [],
            [],
            []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new ScheduleSessionCommand(tenantId, Guid.NewGuid(), DateTime.UtcNow.AddHours(25), 30),
                CancellationToken.None));
    }

    [Fact]
    public async Task Student_cannot_schedule()
    {
        Guid tenantId = Guid.NewGuid();
        ScheduleSessionCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            [Class(tenantId, Guid.NewGuid())],
            [],
            []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new ScheduleSessionCommand(tenantId, Guid.NewGuid(), DateTime.UtcNow.AddHours(25), 30),
                CancellationToken.None));
    }

    private static ScheduleSessionCommandHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid userId,
        List<SchoolClass> classes,
        List<ClassroomSession> captured,
        List<ClassroomScheduledNotification> published)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.SchoolClasses).Returns(classes.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<ClassroomSession>> sessions = new List<ClassroomSession>().AsQueryable().BuildMockDbSet();
        sessions.Setup(s => s.AddAsync(It.IsAny<ClassroomSession>(), It.IsAny<CancellationToken>()))
            .Callback<ClassroomSession, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ClassroomSession>>(
                (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ClassroomSession>)null!));
        db.Setup(x => x.ClassroomSessions).Returns(sessions.Object);

        Mock<IPublisher> publisher = new();
        publisher
            .Setup(p => p.Publish(It.IsAny<ClassroomScheduledNotification>(), It.IsAny<CancellationToken>()))
            .Callback((object notification, CancellationToken _) =>
            {
                if (notification is ClassroomScheduledNotification scheduled)
                    published.Add(scheduled);
            })
            .Returns(Task.CompletedTask);

        return new ScheduleSessionCommandHandler(
            db.Object,
            CurrentUser(tenantId, role, userId).Object,
            publisher.Object,
            NullLogger<ScheduleSessionCommandHandler>.Instance);
    }
}
