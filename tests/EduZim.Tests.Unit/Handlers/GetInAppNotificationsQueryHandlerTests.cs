using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Notifications.DTOs;
using EduZim.Application.Notifications.Queries.GetInAppNotifications;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetInAppNotificationsQueryHandlerTests
{
    [Fact]
    public async Task Returns_only_in_app_rows_for_the_user_newest_first()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        DateTime older = DateTime.UtcNow.AddMinutes(-10);
        DateTime newer = DateTime.UtcNow;
        List<Notification> rows =
        [
            Row(tenantId, userId, NotificationChannel.InApp, "older", older),
            Row(tenantId, userId, NotificationChannel.Email, "email", newer),
            Row(tenantId, userId, NotificationChannel.InApp, "newer", newer),
            Row(tenantId, Guid.NewGuid(), NotificationChannel.InApp, "other user", newer),
        ];
        GetInAppNotificationsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            userId,
            [User(tenantId, userId)],
            rows);

        InAppNotificationsDto dto = await handler.Handle(
            new GetInAppNotificationsQuery(tenantId, userId),
            CancellationToken.None);

        Assert.Equal(userId, dto.UserId);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal("newer", dto.Items[0].Message);
        Assert.Equal("older", dto.Items[1].Message);
    }

    [Fact]
    public async Task Student_cannot_view_another_users_notifications()
    {
        Guid tenantId = Guid.NewGuid();
        GetInAppNotificationsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            [User(tenantId, Guid.NewGuid())],
            []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new GetInAppNotificationsQuery(tenantId, Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task School_admin_can_view_another_users_notifications()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        GetInAppNotificationsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.SchoolAdmin,
            Guid.NewGuid(),
            [User(tenantId, studentId)],
            [Row(tenantId, studentId, NotificationChannel.InApp, "hello", DateTime.UtcNow)]);

        InAppNotificationsDto dto = await handler.Handle(
            new GetInAppNotificationsQuery(tenantId, studentId),
            CancellationToken.None);

        Assert.Single(dto.Items);
    }

    [Fact]
    public async Task Missing_user_throws_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        GetInAppNotificationsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            userId,
            [],
            []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetInAppNotificationsQuery(tenantId, userId), CancellationToken.None));
    }

    private static ApplicationUser User(Guid tenantId, Guid userId)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            Role = UserRole.Student,
        };
    }

    private static Notification Row(
        Guid tenantId,
        Guid userId,
        NotificationChannel channel,
        string message,
        DateTime createdAt)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Type = NotificationType.AssessmentDue,
            Message = message,
            IsRead = false,
            Channel = channel,
            Status = NotificationStatus.Delivered,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    private static GetInAppNotificationsQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid currentUserId,
        List<ApplicationUser> users,
        List<Notification> notifications)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Notifications).Returns(notifications.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(currentUserId);

        return new GetInAppNotificationsQueryHandler(
            db.Object,
            user.Object,
            NullLogger<GetInAppNotificationsQueryHandler>.Instance);
    }
}
