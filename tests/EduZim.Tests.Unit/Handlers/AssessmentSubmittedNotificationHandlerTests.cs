using EduZim.Application.Assessments.Notifications;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class AssessmentSubmittedNotificationHandlerTests
{
    [Fact]
    public async Task Queues_parent_notification_with_score_and_title()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid parentA = Guid.NewGuid();
        Guid parentB = Guid.NewGuid();
        Guid assessmentId = Guid.NewGuid();
        List<QueueNotificationCommand> queued = [];
        AssessmentSubmittedNotificationHandler handler = CreateHandler(
            [
                Student(tenantId, studentId, "Ada Ncube"),
            ],
            [
                Assessment(tenantId, assessmentId, "Fractions quiz"),
            ],
            [
                ProgressTestData.Link(tenantId, parentA, studentId),
                ProgressTestData.Link(tenantId, parentB, studentId),
            ],
            queued);

        await handler.Handle(
            new AssessmentSubmittedNotification(studentId, assessmentId, Guid.NewGuid(), tenantId, 91),
            CancellationToken.None);

        Assert.Equal(2, queued.Count);
        Assert.All(queued, c => Assert.Equal(NotificationType.AssessmentSubmitted, c.Type));
        Assert.All(queued, c => Assert.Equal(tenantId, c.TenantId));
        Assert.Contains(queued, c => c.UserId == parentA);
        Assert.Contains(queued, c => c.UserId == parentB);
        Assert.All(
            queued,
            c => Assert.Equal("Ada Ncube scored 91% on \"Fractions quiz\".", c.Message));
    }

    [Fact]
    public async Task Skips_when_no_linked_parents()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        List<QueueNotificationCommand> queued = [];
        AssessmentSubmittedNotificationHandler handler = CreateHandler(
            [Student(tenantId, studentId, "Ada")],
            [Assessment(tenantId, Guid.NewGuid(), "Quiz")],
            [],
            queued);

        await handler.Handle(
            new AssessmentSubmittedNotification(studentId, Guid.NewGuid(), Guid.NewGuid(), tenantId, 70),
            CancellationToken.None);

        Assert.Empty(queued);
    }

    private static ApplicationUser Student(Guid tenantId, Guid studentId, string fullName)
    {
        return new ApplicationUser
        {
            Id = studentId,
            TenantId = tenantId,
            Role = UserRole.Student,
            FullName = fullName,
            UserName = $"{studentId}@example.com",
            Email = $"{studentId}@example.com",
        };
    }

    private static Assessment Assessment(Guid tenantId, Guid assessmentId, string title)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new Assessment
        {
            Id = assessmentId,
            TenantId = tenantId,
            ModuleId = Guid.NewGuid(),
            Title = title,
            PassingScorePercent = 60,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    private static AssessmentSubmittedNotificationHandler CreateHandler(
        List<ApplicationUser> users,
        List<Assessment> assessments,
        List<ParentStudentLink> links,
        List<QueueNotificationCommand> queued)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Assessments).Returns(assessments.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ParentStudentLinks).Returns(links.AsQueryable().BuildMockDbSet().Object);

        Mock<IMediator> mediator = new();
        mediator
            .Setup(m => m.Send(It.IsAny<QueueNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<MediatR.Unit>, CancellationToken>((req, _) => queued.Add((QueueNotificationCommand)req))
            .ReturnsAsync(MediatR.Unit.Value);

        return new AssessmentSubmittedNotificationHandler(
            db.Object,
            mediator.Object,
            NullLogger<AssessmentSubmittedNotificationHandler>.Instance);
    }
}
