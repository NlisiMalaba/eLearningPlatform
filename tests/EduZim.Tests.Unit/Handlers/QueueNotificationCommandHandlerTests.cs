using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;
using EduZim.Application.Exceptions;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
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
        Mock<INotificationBackgroundJobs> jobs = new();
        QueueNotificationCommandHandler handler = CreateHandler(
            [User(tenantId, userId, "a@example.com", "+263771000000")],
            [Preference(tenantId, userId, NotificationType.LiveClassroomReminder, false, false, true)],
            captured,
            new Mock<IEmailService>(),
            sms,
            jobs);

        await handler.Handle(
            new QueueNotificationCommand(tenantId, userId, NotificationType.LiveClassroomReminder, "Starts soon"),
            CancellationToken.None);

        Notification smsRow = Assert.Single(captured);
        Assert.Equal(NotificationChannel.Sms, smsRow.Channel);
        Assert.Equal(NotificationStatus.Failed, smsRow.Status);
        Assert.Equal(0, smsRow.RetryCount);
        jobs.Verify(j => j.ScheduleSmsRetry(tenantId, smsRow.Id), Times.Once);
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

    [Fact]
    public async Task Student_inactive_notification_queues_alert_for_each_linked_parent()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid parentA = Guid.NewGuid();
        Guid parentB = Guid.NewGuid();
        ApplicationUser student = Student(tenantId, studentId, DateTime.UtcNow.AddDays(-8));
        Mock<IMediator> mediator = new();
        mediator
            .Setup(m => m.Send(It.IsAny<QueueNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MediatR.Unit.Value);
        QueueNotificationCommandHandler handler = CreateHandler(
            [student],
            [],
            [],
            new Mock<IEmailService>(),
            new Mock<ISmsService>(),
            mediator: mediator,
            parentLinks:
            [
                Link(tenantId, parentA, studentId),
                Link(tenantId, parentB, studentId),
            ]);

        await handler.Handle(
            new StudentInactiveNotification(studentId, tenantId, student.LastLoginAt),
            CancellationToken.None);

        mediator.Verify(
            m => m.Send(
                It.Is<QueueNotificationCommand>(
                    c => c.TenantId == tenantId
                        && c.Type == NotificationType.InactivityAlert
                        && c.UserId == parentA
                        && c.Message == InactivityAlertRules.ParentMessage()),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mediator.Verify(
            m => m.Send(
                It.Is<QueueNotificationCommand>(
                    c => c.UserId == parentB && c.Type == NotificationType.InactivityAlert),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(student.LastInactivityAlertAt);
    }

    [Fact]
    public async Task Student_inactive_notification_skips_when_no_linked_parents()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ApplicationUser student = Student(tenantId, studentId, DateTime.UtcNow.AddDays(-8));
        Mock<IMediator> mediator = new();
        QueueNotificationCommandHandler handler = CreateHandler(
            [student],
            [],
            [],
            new Mock<IEmailService>(),
            new Mock<ISmsService>(),
            mediator: mediator);

        await handler.Handle(
            new StudentInactiveNotification(studentId, tenantId, student.LastLoginAt),
            CancellationToken.None);

        mediator.Verify(
            m => m.Send(It.IsAny<QueueNotificationCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Null(student.LastInactivityAlertAt);
    }

    [Fact]
    public async Task Student_inactive_notification_is_idempotent_after_alert()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ApplicationUser student = Student(tenantId, studentId, DateTime.UtcNow.AddDays(-8));
        student.LastInactivityAlertAt = DateTime.UtcNow.AddDays(-1);
        Mock<IMediator> mediator = new();
        QueueNotificationCommandHandler handler = CreateHandler(
            [student],
            [],
            [],
            new Mock<IEmailService>(),
            new Mock<ISmsService>(),
            mediator: mediator,
            parentLinks: [Link(tenantId, Guid.NewGuid(), studentId)]);

        await handler.Handle(
            new StudentInactiveNotification(studentId, tenantId, student.LastLoginAt),
            CancellationToken.None);

        mediator.Verify(
            m => m.Send(It.IsAny<QueueNotificationCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ApplicationUser Student(Guid tenantId, Guid userId, DateTime lastLoginAt)
    {
        ApplicationUser user = User(tenantId, userId, "stu@example.com", null);
        user.LastLoginAt = lastLoginAt;
        return user;
    }

    private static ParentStudentLink Link(Guid tenantId, Guid parentId, Guid studentId)
    {
        DateTime now = DateTime.UtcNow;
        return new ParentStudentLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ParentUserId = parentId,
            StudentUserId = studentId,
            CreatedAt = now,
            UpdatedAt = now,
        };
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
        Mock<ISmsService> sms,
        Mock<INotificationBackgroundJobs>? jobs = null,
        Mock<IMediator>? mediator = null,
        List<ParentStudentLink>? parentLinks = null)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.NotificationPreferences).Returns(preferences.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ParentStudentLinks)
            .Returns((parentLinks ?? []).AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<Notification>> notifications = new List<Notification>().AsQueryable().BuildMockDbSet();
        notifications.Setup(s => s.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Notification>>(
                (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Notification>)null!));
        db.Setup(x => x.Notifications).Returns(notifications.Object);

        Mock<INotificationBackgroundJobs> backgroundJobs = jobs ?? new Mock<INotificationBackgroundJobs>();
        Mock<IMediator> mediatorMock = mediator ?? new Mock<IMediator>();
        return new QueueNotificationCommandHandler(
            db.Object,
            email.Object,
            sms.Object,
            backgroundJobs.Object,
            mediatorMock.Object,
            NullLogger<QueueNotificationCommandHandler>.Instance);
    }
}
