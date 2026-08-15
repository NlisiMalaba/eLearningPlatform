using EduZim.Application.Billing.Notifications;
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

public sealed class PaymentNotificationHandlerTests
{
    [Fact]
    public async Task Payment_succeeded_restores_suspended_tenant_and_notifies_subscribers()
    {
        Guid tenantId = Guid.NewGuid();
        Guid adminId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        Tenant tenant = SuspendedTenant(tenantId, "Mufakose Primary");
        List<QueueNotificationCommand> queued = [];
        Mock<ITenantBackgroundJobs> jobs = new();
        PaymentSucceededNotificationHandler handler = CreateSucceededHandler(
            tenant,
            [Admin(tenantId, adminId), Parent(tenantId, parentId)],
            jobs,
            queued);

        await handler.Handle(
            new PaymentSucceededNotification(tenantId, Guid.NewGuid(), "pi_1", "idem-1"),
            CancellationToken.None);

        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Null(tenant.SuspendedAtUtc);
        Assert.Null(tenant.PermanentDeletionHangfireJobId);
        jobs.Verify(j => j.TryCancelJob("del-job"), Times.Once);
        Assert.Equal(2, queued.Count);
        Assert.All(queued, c => Assert.Equal(NotificationType.PaymentSucceeded, c.Type));
        Assert.Contains(queued, c => c.UserId == adminId);
        Assert.Contains(queued, c => c.UserId == parentId);
        Assert.All(queued, c => Assert.Contains("Mufakose Primary", c.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Payment_failed_notifies_subscribers_without_suspending_tenant()
    {
        Guid tenantId = Guid.NewGuid();
        Guid adminId = Guid.NewGuid();
        Tenant tenant = ProgressTestData.ActiveTenant(tenantId);
        tenant.Name = "Mufakose Primary";
        List<QueueNotificationCommand> queued = [];
        PaymentFailedNotificationHandler handler = CreateFailedHandler(
            tenant,
            [Admin(tenantId, adminId)],
            queued);

        await handler.Handle(
            new PaymentFailedNotification(tenantId, Guid.NewGuid(), "card_declined", "in_1"),
            CancellationToken.None);

        Assert.Equal(TenantStatus.Active, tenant.Status);
        QueueNotificationCommand notice = Assert.Single(queued);
        Assert.Equal(adminId, notice.UserId);
        Assert.Equal(NotificationType.PaymentFailed, notice.Type);
        Assert.Contains("card_declined", notice.Message, StringComparison.Ordinal);
        Assert.Contains("7-day grace period", notice.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Payment_failed_skips_queue_when_no_subscribers()
    {
        Guid tenantId = Guid.NewGuid();
        List<QueueNotificationCommand> queued = [];
        PaymentFailedNotificationHandler handler = CreateFailedHandler(
            ProgressTestData.ActiveTenant(tenantId),
            [],
            queued);

        await handler.Handle(
            new PaymentFailedNotification(tenantId, Guid.NewGuid(), null, null),
            CancellationToken.None);

        Assert.Empty(queued);
    }

    private static Tenant SuspendedTenant(Guid tenantId, string name)
    {
        Tenant tenant = ProgressTestData.ActiveTenant(tenantId);
        tenant.Name = name;
        tenant.Status = TenantStatus.Suspended;
        tenant.SuspendedAtUtc = DateTime.UtcNow.AddDays(-2);
        tenant.PermanentDeletionHangfireJobId = "del-job";
        return tenant;
    }

    private static ApplicationUser Admin(Guid tenantId, Guid userId) =>
        User(tenantId, userId, UserRole.SchoolAdmin);

    private static ApplicationUser Parent(Guid tenantId, Guid userId) =>
        User(tenantId, userId, UserRole.ParentGuardian);

    private static ApplicationUser User(Guid tenantId, Guid userId, UserRole role)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            Role = role,
            Email = $"{userId}@example.com",
            UserName = $"{userId}@example.com",
        };
    }

    private static PaymentSucceededNotificationHandler CreateSucceededHandler(
        Tenant tenant,
        List<ApplicationUser> users,
        Mock<ITenantBackgroundJobs> jobs,
        List<QueueNotificationCommand> queued)
    {
        Mock<IEduZimDbContext> db = CreateDb(tenant, users);
        return new PaymentSucceededNotificationHandler(
            db.Object,
            jobs.Object,
            CreateMediator(queued),
            NullLogger<PaymentSucceededNotificationHandler>.Instance);
    }

    private static PaymentFailedNotificationHandler CreateFailedHandler(
        Tenant tenant,
        List<ApplicationUser> users,
        List<QueueNotificationCommand> queued)
    {
        Mock<IEduZimDbContext> db = CreateDb(tenant, users);
        return new PaymentFailedNotificationHandler(
            db.Object,
            CreateMediator(queued),
            NullLogger<PaymentFailedNotificationHandler>.Instance);
    }

    private static Mock<IEduZimDbContext> CreateDb(Tenant tenant, List<ApplicationUser> users)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Tenants).Returns(new List<Tenant> { tenant }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);
        return db;
    }

    private static IMediator CreateMediator(List<QueueNotificationCommand> queued)
    {
        Mock<IMediator> mediator = new();
        mediator
            .Setup(m => m.Send(It.IsAny<QueueNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<MediatR.Unit>, CancellationToken>((req, _) => queued.Add((QueueNotificationCommand)req))
            .ReturnsAsync(MediatR.Unit.Value);
        return mediator.Object;
    }
}
