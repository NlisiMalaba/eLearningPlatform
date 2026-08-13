using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;
using EduZim.Application.Notifications.Commands.RetryFailedSms;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class RetryFailedSmsCommandHandlerTests
{
    [Fact]
    public async Task Failed_retry_increments_count_and_schedules_next()
    {
        Guid tenantId = Guid.NewGuid();
        Notification notification = SmsRow(tenantId, retryCount: 0, NotificationStatus.Failed);
        Mock<INotificationBackgroundJobs> jobs = new();
        RetryFailedSmsCommandHandler handler = CreateHandler(
            [User(tenantId, notification.UserId, "+263771000000")],
            [notification],
            FailedSms(),
            jobs);

        await handler.Handle(new RetryFailedSmsCommand(tenantId, notification.Id), CancellationToken.None);

        Assert.Equal(1, notification.RetryCount);
        Assert.Equal(NotificationStatus.Failed, notification.Status);
        jobs.Verify(j => j.ScheduleSmsRetry(tenantId, notification.Id), Times.Once);
    }

    [Fact]
    public async Task Third_failed_retry_marks_undelivered_and_does_not_schedule()
    {
        Guid tenantId = Guid.NewGuid();
        Notification notification = SmsRow(tenantId, retryCount: 2, NotificationStatus.Failed);
        Mock<INotificationBackgroundJobs> jobs = new();
        RetryFailedSmsCommandHandler handler = CreateHandler(
            [User(tenantId, notification.UserId, "+263771000000")],
            [notification],
            FailedSms(),
            jobs);

        await handler.Handle(new RetryFailedSmsCommand(tenantId, notification.Id), CancellationToken.None);

        Assert.Equal(SmsRetryRules.MaxRetryAttempts, notification.RetryCount);
        Assert.Equal(NotificationStatus.Undelivered, notification.Status);
        jobs.Verify(j => j.ScheduleSmsRetry(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Successful_retry_marks_delivered()
    {
        Guid tenantId = Guid.NewGuid();
        Notification notification = SmsRow(tenantId, retryCount: 1, NotificationStatus.Failed);
        Mock<INotificationBackgroundJobs> jobs = new();
        RetryFailedSmsCommandHandler handler = CreateHandler(
            [User(tenantId, notification.UserId, "+263771000000")],
            [notification],
            new SmsResult { Success = true },
            jobs);

        await handler.Handle(new RetryFailedSmsCommand(tenantId, notification.Id), CancellationToken.None);

        Assert.Equal(2, notification.RetryCount);
        Assert.Equal(NotificationStatus.Delivered, notification.Status);
        jobs.Verify(j => j.ScheduleSmsRetry(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Already_delivered_is_not_retried()
    {
        Guid tenantId = Guid.NewGuid();
        Notification notification = SmsRow(tenantId, retryCount: 1, NotificationStatus.Delivered);
        Mock<ISmsService> sms = new();
        Mock<INotificationBackgroundJobs> jobs = new();
        RetryFailedSmsCommandHandler handler = CreateHandler(
            [User(tenantId, notification.UserId, "+263771000000")],
            [notification],
            new SmsResult { Success = true },
            jobs,
            sms);

        await handler.Handle(new RetryFailedSmsCommand(tenantId, notification.Id), CancellationToken.None);

        Assert.Equal(1, notification.RetryCount);
        Assert.Equal(NotificationStatus.Delivered, notification.Status);
        sms.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        jobs.Verify(j => j.ScheduleSmsRetry(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Exhausted_retry_count_marks_undelivered_without_sending()
    {
        Guid tenantId = Guid.NewGuid();
        Notification notification = SmsRow(
            tenantId,
            retryCount: SmsRetryRules.MaxRetryAttempts,
            NotificationStatus.Failed);
        Mock<ISmsService> sms = new();
        RetryFailedSmsCommandHandler handler = CreateHandler(
            [User(tenantId, notification.UserId, "+263771000000")],
            [notification],
            FailedSms(),
            new Mock<INotificationBackgroundJobs>(),
            sms);

        await handler.Handle(new RetryFailedSmsCommand(tenantId, notification.Id), CancellationToken.None);

        Assert.Equal(SmsRetryRules.MaxRetryAttempts, notification.RetryCount);
        Assert.Equal(NotificationStatus.Undelivered, notification.Status);
        sms.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Missing_notification_does_not_throw()
    {
        RetryFailedSmsCommandHandler handler = CreateHandler(
            [],
            [],
            FailedSms(),
            new Mock<INotificationBackgroundJobs>());

        await handler.Handle(new RetryFailedSmsCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
    }

    private static ApplicationUser User(Guid tenantId, Guid userId, string phone)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            PhoneNumber = phone,
            Role = UserRole.Student,
        };
    }

    private static Notification SmsRow(Guid tenantId, int retryCount, NotificationStatus status)
    {
        DateTime now = DateTime.UtcNow;
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = Guid.NewGuid(),
            Type = NotificationType.SubscriptionExpiry,
            Message = "Expires soon",
            Channel = NotificationChannel.Sms,
            Status = status,
            RetryCount = retryCount,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static SmsResult FailedSms() => new() { Success = false, ErrorMessage = "gateway down" };

    private static RetryFailedSmsCommandHandler CreateHandler(
        List<ApplicationUser> users,
        List<Notification> notifications,
        SmsResult smsResult,
        Mock<INotificationBackgroundJobs> jobs,
        Mock<ISmsService>? sms = null)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Notifications).Returns(notifications.AsQueryable().BuildMockDbSet().Object);

        Mock<ISmsService> smsService = sms ?? new Mock<ISmsService>();
        smsService
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(smsResult);

        return new RetryFailedSmsCommandHandler(
            db.Object,
            smsService.Object,
            jobs.Object,
            NullLogger<RetryFailedSmsCommandHandler>.Instance);
    }
}
