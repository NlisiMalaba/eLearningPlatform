using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Notifications.Commands.MarkNotificationRead;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class MarkNotificationReadCommandHandlerTests
{
    [Fact]
    public async Task Marks_unread_in_app_notification_as_read()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Notification row = Row(tenantId, userId, isRead: false);
        MarkNotificationReadCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            userId,
            [row]);

        await handler.Handle(new MarkNotificationReadCommand(tenantId, row.Id), CancellationToken.None);

        Assert.True(row.IsRead);
    }

    [Fact]
    public async Task Already_read_notification_is_unchanged()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Notification row = Row(tenantId, userId, isRead: true);
        DateTime updatedAt = row.UpdatedAt;
        MarkNotificationReadCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            userId,
            [row]);

        await handler.Handle(new MarkNotificationReadCommand(tenantId, row.Id), CancellationToken.None);

        Assert.True(row.IsRead);
        Assert.Equal(updatedAt, row.UpdatedAt);
    }

    [Fact]
    public async Task Student_cannot_mark_another_users_notification()
    {
        Guid tenantId = Guid.NewGuid();
        Notification row = Row(tenantId, Guid.NewGuid(), isRead: false);
        MarkNotificationReadCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            [row]);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new MarkNotificationReadCommand(tenantId, row.Id), CancellationToken.None));
        Assert.False(row.IsRead);
    }

    [Fact]
    public async Task Missing_notification_throws_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        MarkNotificationReadCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            userId,
            []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new MarkNotificationReadCommand(tenantId, Guid.NewGuid()), CancellationToken.None));
    }

    private static Notification Row(Guid tenantId, Guid userId, bool isRead)
    {
        DateTime now = DateTime.UtcNow;
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Type = NotificationType.AssessmentDue,
            Message = "Due tomorrow",
            IsRead = isRead,
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Delivered,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static MarkNotificationReadCommandHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid currentUserId,
        List<Notification> notifications)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Notifications).Returns(notifications.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(currentUserId);

        return new MarkNotificationReadCommandHandler(
            db.Object,
            user.Object,
            NullLogger<MarkNotificationReadCommandHandler>.Instance);
    }
}
