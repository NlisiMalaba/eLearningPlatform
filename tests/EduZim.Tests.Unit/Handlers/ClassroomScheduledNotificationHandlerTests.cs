using EduZim.Application.Common.Interfaces;
using EduZim.Application.LiveClassrooms.Notifications;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using static EduZim.Tests.Unit.Handlers.LiveClassroomTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ClassroomScheduledNotificationHandlerTests
{
    [Fact]
    public async Task Queues_reminder_for_each_student_and_linked_parent()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        DateTime start = DateTime.UtcNow.AddHours(26);
        List<QueueNotificationCommand> queued = [];
        ClassroomScheduledNotificationHandler handler = CreateHandler(
            [Session(tenantId, sessionId, classId, Guid.NewGuid(), start)],
            [Class(tenantId, classId, "4A")],
            [Enrollment(tenantId, classId, studentId)],
            [Link(tenantId, parentId, studentId)],
            queued);

        await handler.Handle(new ClassroomScheduledNotification(sessionId, tenantId), CancellationToken.None);

        Assert.Equal(2, queued.Count);
        Assert.All(queued, c => Assert.Equal(NotificationType.LiveClassroomReminder, c.Type));
        Assert.Contains(queued, c => c.UserId == studentId);
        Assert.Contains(queued, c => c.UserId == parentId);
        Assert.True(queued[0].Message.Contains("4A", StringComparison.Ordinal));
        DateTime notifyBy = start - TimeSpan.FromHours(24);
        Assert.True(DateTime.UtcNow <= notifyBy);
    }

    [Fact]
    public async Task Skips_when_no_enrolled_students()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        List<QueueNotificationCommand> queued = [];
        ClassroomScheduledNotificationHandler handler = CreateHandler(
            [Session(tenantId, sessionId, classId, Guid.NewGuid(), DateTime.UtcNow.AddDays(2))],
            [Class(tenantId, classId)],
            [],
            [],
            queued);

        await handler.Handle(new ClassroomScheduledNotification(sessionId, tenantId), CancellationToken.None);

        Assert.Empty(queued);
    }

    private static ParentStudentLink Link(Guid tenantId, Guid parentId, Guid studentId)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ParentStudentLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ParentUserId = parentId,
            StudentUserId = studentId,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    private static ClassroomScheduledNotificationHandler CreateHandler(
        List<ClassroomSession> sessions,
        List<SchoolClass> classes,
        List<ClassEnrollment> enrollments,
        List<ParentStudentLink> links,
        List<QueueNotificationCommand> queued)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.ClassroomSessions).Returns(sessions.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.SchoolClasses).Returns(classes.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ClassEnrollments).Returns(enrollments.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ParentStudentLinks).Returns(links.AsQueryable().BuildMockDbSet().Object);

        Mock<IMediator> mediator = new();
        mediator
            .Setup(m => m.Send(It.IsAny<QueueNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<MediatR.Unit>, CancellationToken>((req, _) => queued.Add((QueueNotificationCommand)req))
            .ReturnsAsync(MediatR.Unit.Value);

        return new ClassroomScheduledNotificationHandler(
            db.Object,
            mediator.Object,
            NullLogger<ClassroomScheduledNotificationHandler>.Instance);
    }
}
