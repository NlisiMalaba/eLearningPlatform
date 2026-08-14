using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Application.Progress.Commands.SendParentWeeklySummaries;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class SendParentWeeklySummariesCommandHandlerTests
{
    [Fact]
    public async Task Queues_weekly_summary_for_parent_of_linked_student()
    {
        Guid tenantId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        List<QueueNotificationCommand> queued = [];
        SendParentWeeklySummariesCommandHandler handler = CreateHandler(
            [ProgressTestData.ActiveTenant(tenantId)],
            [ProgressTestData.Link(tenantId, parentId, studentId)],
            [ProgressTestData.Module(tenantId, moduleId, "Math", 1)],
            [ProgressTestData.Progress(tenantId, studentId, moduleId, true, true, DateTime.UtcNow)],
            badges: [],
            notifications: [],
            users: [Student(tenantId, studentId, "Ada")],
            queued);

        await handler.Handle(new SendParentWeeklySummariesCommand(), CancellationToken.None);

        QueueNotificationCommand notice = Assert.Single(queued);
        Assert.Equal(parentId, notice.UserId);
        Assert.Equal(NotificationType.WeeklyProgressSummary, notice.Type);
        Assert.Contains("Ada", notice.Message, StringComparison.Ordinal);
        Assert.Contains("1 module(s) completed", notice.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Skips_parent_already_notified_this_week()
    {
        Guid tenantId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        List<QueueNotificationCommand> queued = [];
        Notification existing = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = parentId,
            Type = NotificationType.WeeklyProgressSummary,
            Message = "already sent",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        SendParentWeeklySummariesCommandHandler handler = CreateHandler(
            [ProgressTestData.ActiveTenant(tenantId)],
            [ProgressTestData.Link(tenantId, parentId, studentId)],
            [ProgressTestData.Module(tenantId, Guid.NewGuid(), "Math", 1)],
            progresses: [],
            badges: [],
            notifications: [existing],
            users: [],
            queued);

        await handler.Handle(new SendParentWeeklySummariesCommand(), CancellationToken.None);

        Assert.Empty(queued);
    }

    [Fact]
    public async Task Skips_suspended_tenants()
    {
        Guid tenantId = Guid.NewGuid();
        Tenant suspended = ProgressTestData.ActiveTenant(tenantId);
        suspended.Status = TenantStatus.Suspended;
        List<QueueNotificationCommand> queued = [];
        SendParentWeeklySummariesCommandHandler handler = CreateHandler(
            [suspended],
            [ProgressTestData.Link(tenantId, Guid.NewGuid(), Guid.NewGuid())],
            modules: [],
            progresses: [],
            badges: [],
            notifications: [],
            users: [],
            queued);

        await handler.Handle(new SendParentWeeklySummariesCommand(), CancellationToken.None);

        Assert.Empty(queued);
    }

    private static ApplicationUser Student(Guid tenantId, Guid id, string fullName)
    {
        return new ApplicationUser
        {
            Id = id,
            TenantId = tenantId,
            Role = UserRole.Student,
            FullName = fullName,
        };
    }

    private static SendParentWeeklySummariesCommandHandler CreateHandler(
        List<Tenant> tenants,
        List<ParentStudentLink> links,
        List<Module> modules,
        List<StudentProgress> progresses,
        List<Badge> badges,
        List<Notification> notifications,
        List<ApplicationUser> users,
        List<QueueNotificationCommand> queued)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Tenants).Returns(tenants.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ParentStudentLinks).Returns(links.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Modules).Returns(modules.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.StudentProgresses).Returns(progresses.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Badges).Returns(badges.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Notifications).Returns(notifications.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);

        Mock<IMediator> mediator = new();
        mediator
            .Setup(m => m.Send(It.IsAny<QueueNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<MediatR.Unit>, CancellationToken>((req, _) => queued.Add((QueueNotificationCommand)req))
            .ReturnsAsync(MediatR.Unit.Value);

        return new SendParentWeeklySummariesCommandHandler(
            db.Object,
            mediator.Object,
            NullLogger<SendParentWeeklySummariesCommandHandler>.Instance);
    }
}
