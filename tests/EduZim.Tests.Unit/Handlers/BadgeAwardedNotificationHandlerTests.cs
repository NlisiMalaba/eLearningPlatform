using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.Notifications;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class BadgeAwardedNotificationHandlerTests
{
    [Fact]
    public async Task Queues_certificate_and_notifies_linked_parents()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid badgeId = Guid.NewGuid();
        Guid parentA = Guid.NewGuid();
        Guid parentB = Guid.NewGuid();
        List<QueueNotificationCommand> queued = [];
        RecordingJobs jobs = new();
        BadgeAwardedNotificationHandler handler = CreateHandler(
            [Student(tenantId, studentId, "Chipo Moyo")],
            [
                ProgressTestData.Link(tenantId, parentA, studentId),
                ProgressTestData.Link(tenantId, parentB, studentId),
            ],
            jobs,
            queued);

        await handler.Handle(
            new BadgeAwardedNotification(studentId, badgeId, tenantId, BadgeType.SubjectMastery),
            CancellationToken.None);

        Assert.Equal((tenantId, studentId, badgeId), Assert.Single(jobs.Queued));
        Assert.Equal(2, queued.Count);
        Assert.All(queued, c => Assert.Equal(NotificationType.BadgeAwarded, c.Type));
        Assert.All(queued, c => Assert.Equal(tenantId, c.TenantId));
        Assert.Contains(queued, c => c.UserId == parentA);
        Assert.Contains(queued, c => c.UserId == parentB);
        Assert.All(
            queued,
            c => Assert.Equal("Chipo Moyo earned the subject mastery badge.", c.Message));
    }

    [Fact]
    public async Task Still_queues_certificate_when_no_linked_parents()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid badgeId = Guid.NewGuid();
        List<QueueNotificationCommand> queued = [];
        RecordingJobs jobs = new();
        BadgeAwardedNotificationHandler handler = CreateHandler(
            [Student(tenantId, studentId, "Chipo")],
            [],
            jobs,
            queued);

        await handler.Handle(
            new BadgeAwardedNotification(studentId, badgeId, tenantId, BadgeType.FirstModule),
            CancellationToken.None);

        Assert.Equal((tenantId, studentId, badgeId), Assert.Single(jobs.Queued));
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

    private static BadgeAwardedNotificationHandler CreateHandler(
        List<ApplicationUser> users,
        List<ParentStudentLink> links,
        RecordingJobs jobs,
        List<QueueNotificationCommand> queued)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ParentStudentLinks).Returns(links.AsQueryable().BuildMockDbSet().Object);

        Mock<IMediator> mediator = new();
        mediator
            .Setup(m => m.Send(It.IsAny<QueueNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<MediatR.Unit>, CancellationToken>((req, _) => queued.Add((QueueNotificationCommand)req))
            .ReturnsAsync(MediatR.Unit.Value);

        return new BadgeAwardedNotificationHandler(
            db.Object,
            jobs,
            mediator.Object,
            NullLogger<BadgeAwardedNotificationHandler>.Instance);
    }

    private sealed class RecordingJobs : IGamificationBackgroundJobs
    {
        public List<(Guid TenantId, Guid StudentId, Guid BadgeId)> Queued { get; } = [];

        public string? EnqueueCertificateGeneration(Guid tenantId, Guid studentId, Guid badgeId)
        {
            Queued.Add((tenantId, studentId, badgeId));
            return "job-id";
        }
    }
}
