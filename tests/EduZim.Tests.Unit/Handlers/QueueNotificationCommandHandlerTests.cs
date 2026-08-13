using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;
using EduZim.Application.Exceptions;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class QueueNotificationCommandHandlerTests
{
    [Fact]
    public async Task Default_preferences_dispatch_in_app_and_email_but_not_sms()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        List<Notification> captured = [];
        Mock<IEmailService> email = new();
        Mock<ISmsService> sms = new();
        QueueNotificationCommandHandler handler = CreateHandler(
            [User(tenantId, userId, "a@example.com", "+263771000000")],
            [],
            captured,
            email,
            sms);

        await handler.Handle(
            new QueueNotificationCommand(tenantId, userId, NotificationType.AssessmentDue, "Due tomorrow"),
            CancellationToken.None);

        Assert.Equal(2, captured.Count);
        Assert.Contains(captured, n => n.Channel == NotificationChannel.InApp && n.Status == NotificationStatus.Delivered);
        Assert.Contains(captured, n => n.Channel == NotificationChannel.Email && n.Status == NotificationStatus.Delivered);
        Assert.DoesNotContain(captured, n => n.Channel == NotificationChannel.Sms);
        email.Verify(
            s => s.SendAsync("a@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        sms.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Critical_type_dispatches_sms_when_phone_is_registered()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        List<Notification> captured = [];
        Mock<ISmsService> sms = new();
        sms.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult { Success = true });
        QueueNotificationCommandHandler handler = CreateHandler(
            [User(tenantId, userId, "a@example.com", "+263771000000")],
            [],
            captured,
            new Mock<IEmailService>(),
            sms);

        await handler.Handle(
            new QueueNotificationCommand(tenantId, userId, NotificationType.SubscriptionExpiry, "Expires soon"),
            CancellationToken.None);

        Assert.Contains(captured, n => n.Channel == NotificationChannel.Sms && n.Status == NotificationStatus.Delivered);
        sms.Verify(s => s.SendAsync("+263771000000", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Disabled_channels_are_not_dispatched()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        List<Notification> captured = [];
        Mock<IEmailService> email = new();
        Mock<ISmsService> sms = new();
        NotificationPreference preference = Preference(tenantId, userId, NotificationType.BadgeAwarded, false, false, false);
        QueueNotificationCommandHandler handler = CreateHandler(
            [User(tenantId, userId, "a@example.com", "+263771000000")],
            [preference],
            captured,
            email,
            sms);

        await handler.Handle(
            new QueueNotificationCommand(tenantId, userId, NotificationType.BadgeAwarded, "You earned a badge"),
            CancellationToken.None);

        Assert.Empty(captured);
        email.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        sms.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Enabled_in_app_only_stores_in_app_row()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        List<Notification> captured = [];
        NotificationPreference preference = Preference(tenantId, userId, NotificationType.NewContent, true, false, false);
        QueueNotificationCommandHandler handler = CreateHandler(
            [User(tenantId, userId, "a@example.com", "+263771000000")],
            [preference],
            captured,
            new Mock<IEmailService>(),
            new Mock<ISmsService>());

        await handler.Handle(
            new QueueNotificationCommand(tenantId, userId, NotificationType.NewContent, "New module"),
            CancellationToken.None);

        Assert.Single(captured);
        Assert.Equal(NotificationChannel.InApp, captured[0].Channel);
        Assert.Equal(tenantId, captured[0].TenantId);
        Assert.Equal(userId, captured[0].UserId);
    }

    [Fact]
    public async Task Email_failure_persists_failed_status()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        List<Notification> captured = [];
        Mock<IEmailService> email = new();
        email.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp unavailable"));
        QueueNotificationCommandHandler handler = CreateHandler(
            [User(tenantId, userId, "a@example.com", null)],
            [],
            captured,
            email,
            new Mock<ISmsService>());

        await handler.Handle(
            new QueueNotificationCommand(tenantId, userId, NotificationType.AssessmentDue, "Due tomorrow"),
            CancellationToken.None);

        Notification emailRow = Assert.Single(captured, n => n.Channel == NotificationChannel.Email);
        Assert.Equal(NotificationStatus.Failed, emailRow.Status);
    }

    [Fact]
    public async Task Sms_failure_persists_failed_status()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        List<Notification> captured = [];
        Mock<ISmsService> sms = new();
        sms.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsResult { Success = false, ErrorMessage = "gateway down" });
        QueueNotificationCommandHandler handler = CreateHandler(
            [User(tenantId, userId, "a@example.com", "+263771000000")],
            [Preference(tenantId, userId, NotificationType.LiveClassroomReminder, false, false, true)],
            captured,
            new Mock<IEmailService>(),
            sms);

        await handler.Handle(
            new QueueNotificationCommand(tenantId, userId, NotificationType.LiveClassroomReminder, "Starts soon"),
            CancellationToken.None);

        Notification smsRow = Assert.Single(captured);
        Assert.Equal(NotificationChannel.Sms, smsRow.Channel);
        Assert.Equal(NotificationStatus.Failed, smsRow.Status);
        Assert.Equal(0, smsRow.RetryCount);
    }

    [Fact]
    public async Task Missing_user_throws_not_found()
    {
        QueueNotificationCommandHandler handler = CreateHandler(
            [],
            [],
            [],
            new Mock<IEmailService>(),
            new Mock<ISmsService>());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new QueueNotificationCommand(Guid.NewGuid(), Guid.NewGuid(), NotificationType.NewContent, "Hello"),
                CancellationToken.None));
    }

    [Fact]
    public async Task User_in_another_tenant_is_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        QueueNotificationCommandHandler handler = CreateHandler(
            [User(Guid.NewGuid(), userId, "a@example.com", null)],
            [],
            [],
            new Mock<IEmailService>(),
            new Mock<ISmsService>());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new QueueNotificationCommand(tenantId, userId, NotificationType.NewContent, "Hello"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Sms_skipped_when_no_phone_number()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        List<Notification> captured = [];
        Mock<ISmsService> sms = new();
        QueueNotificationCommandHandler handler = CreateHandler(
            [User(tenantId, userId, "a@example.com", null)],
            [],
            captured,
            new Mock<IEmailService>(),
            sms);

        await handler.Handle(
            new QueueNotificationCommand(tenantId, userId, NotificationType.SubscriptionExpiry, "Expires soon"),
            CancellationToken.None);

        Assert.DoesNotContain(captured, n => n.Channel == NotificationChannel.Sms);
        sms.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ApplicationUser User(Guid tenantId, Guid userId, string? email, string? phone)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            Email = email,
            PhoneNumber = phone,
            Role = UserRole.Student,
        };
    }

    private static NotificationPreference Preference(
        Guid tenantId,
        Guid userId,
        NotificationType type,
        bool inApp,
        bool email,
        bool sms)
    {
        DateTime now = DateTime.UtcNow;
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Type = type,
            InAppEnabled = inApp,
            EmailEnabled = email,
            SmsEnabled = sms,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static QueueNotificationCommandHandler CreateHandler(
        List<ApplicationUser> users,
        List<NotificationPreference> preferences,
        List<Notification> captured,
        Mock<IEmailService> email,
        Mock<ISmsService> sms)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.NotificationPreferences).Returns(preferences.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<Notification>> notifications = new List<Notification>().AsQueryable().BuildMockDbSet();
        notifications.Setup(s => s.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Notification>>(
                (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Notification>)null!));
        db.Setup(x => x.Notifications).Returns(notifications.Object);

        return new QueueNotificationCommandHandler(
            db.Object,
            email.Object,
            sms.Object,
            NullLogger<QueueNotificationCommandHandler>.Instance);
    }
}
