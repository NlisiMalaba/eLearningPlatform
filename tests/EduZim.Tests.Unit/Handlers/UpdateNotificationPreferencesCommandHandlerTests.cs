using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.UpdateNotificationPreferences;
using EduZim.Application.Notifications.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class UpdateNotificationPreferencesCommandHandlerTests
{
    [Fact]
    public async Task Creates_preference_rows_and_returns_merged_defaults()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        List<NotificationPreference> stored = [];
        UpdateNotificationPreferencesCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            userId,
            [User(tenantId, userId)],
            stored);

        NotificationPreferencesDto dto = await handler.Handle(
            new UpdateNotificationPreferencesCommand(
                tenantId,
                userId,
                [new NotificationPreferenceItemDto(NotificationType.AssessmentDue, true, false, false)]),
            CancellationToken.None);

        Assert.Single(stored);
        Assert.Equal(NotificationType.AssessmentDue, stored[0].Type);
        Assert.True(stored[0].InAppEnabled);
        Assert.False(stored[0].EmailEnabled);
        Assert.Contains(dto.Preferences, p => p.Type == NotificationType.AssessmentDue && !p.EmailEnabled);
        Assert.Contains(dto.Preferences, p => p.Type == NotificationType.NewContent && p.EmailEnabled);
    }

    [Fact]
    public async Task Updates_existing_preference_row()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        NotificationPreference existing = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Type = NotificationType.BadgeAwarded,
            InAppEnabled = true,
            EmailEnabled = true,
            SmsEnabled = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        UpdateNotificationPreferencesCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            userId,
            [User(tenantId, userId)],
            [existing]);

        await handler.Handle(
            new UpdateNotificationPreferencesCommand(
                tenantId,
                userId,
                [new NotificationPreferenceItemDto(NotificationType.BadgeAwarded, false, true, true)]),
            CancellationToken.None);

        Assert.False(existing.InAppEnabled);
        Assert.True(existing.EmailEnabled);
        Assert.True(existing.SmsEnabled);
    }

    [Fact]
    public async Task Student_cannot_update_another_users_preferences()
    {
        Guid tenantId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();
        UpdateNotificationPreferencesCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            [User(tenantId, targetUserId)],
            []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new UpdateNotificationPreferencesCommand(
                    tenantId,
                    targetUserId,
                    [new NotificationPreferenceItemDto(NotificationType.NewContent, true, true, false)]),
                CancellationToken.None));
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

    private static UpdateNotificationPreferencesCommandHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid currentUserId,
        List<ApplicationUser> users,
        List<NotificationPreference> preferences)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<NotificationPreference>> set = preferences.AsQueryable().BuildMockDbSet();
        set.Setup(s => s.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationPreference, CancellationToken>((entity, _) => preferences.Add(entity))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<NotificationPreference>>(
                (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<NotificationPreference>)null!));
        db.Setup(x => x.NotificationPreferences).Returns(set.Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(currentUserId);

        return new UpdateNotificationPreferencesCommandHandler(
            db.Object,
            user.Object,
            NullLogger<UpdateNotificationPreferencesCommandHandler>.Instance);
    }
}
