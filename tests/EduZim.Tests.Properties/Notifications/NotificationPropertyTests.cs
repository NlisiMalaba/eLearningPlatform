using EduZim.Application.Notifications.Commands.PublishInactiveStudentAlerts;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Application.Notifications.Commands.RetryFailedSms;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using FsCheck.Xunit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Notifications;

/// <summary>Feature: elearning-app-zimbabwe — Notification properties 32, 40, 41.</summary>
public sealed class NotificationPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 32: Inactivity Notification After 7 Days — Validates: Requirements 10.6
    [Property(MaxTest = 100)]
    public async Task Property32_inactive_student_queues_inactivity_alert_for_each_linked_parent(
        byte parentCountRaw,
        byte extraDaysRaw)
    {
        int parentCount = (parentCountRaw % 5) + 1;
        int daysInactive = 8 + (extraDaysRaw % 20);
        using ServiceProvider provider = NotificationPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Guid tenantId = Guid.NewGuid();
        List<Guid> parentIds = await NotificationPropertySeeds
            .SeedInactiveFamilyAsync(db, tenantId, parentCount, daysInactive)
            .ConfigureAwait(false);

        await mediator.Send(new PublishInactiveStudentAlertsCommand(), CancellationToken.None)
            .ConfigureAwait(false);

        List<Guid> alerted = await db.Notifications.AsNoTracking()
            .Where(n => n.TenantId == tenantId && n.Type == NotificationType.InactivityAlert)
            .Select(n => n.UserId)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false);

        Assert.Equal(parentIds.OrderBy(id => id), alerted.OrderBy(id => id));
    }

    // Feature: elearning-app-zimbabwe, Property 40: Notification Routing Respects User Preferences — Validates: Requirements 15.4
    [Property(MaxTest = 100)]
    public async Task Property40_only_enabled_channels_receive_the_notification(
        byte typeRaw,
        bool inApp,
        bool email,
        bool sms)
    {
        NotificationType type = (NotificationType)(typeRaw % Enum.GetValues<NotificationType>().Length);
        using ServiceProvider provider = NotificationPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        RecordingEmailService emailLog = provider.GetRequiredService<RecordingEmailService>();
        RecordingSmsService smsLog = provider.GetRequiredService<RecordingSmsService>();

        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await NotificationPropertySeeds
            .SeedNotifiableUserAsync(db, tenantId, userId, type, inApp, email, sms)
            .ConfigureAwait(false);

        await mediator
            .Send(new QueueNotificationCommand(tenantId, userId, type, "Alert body"), CancellationToken.None)
            .ConfigureAwait(false);

        List<NotificationChannel> dispatched = await db.Notifications.AsNoTracking()
            .Where(n => n.TenantId == tenantId && n.UserId == userId)
            .Select(n => n.Channel)
            .ToListAsync()
            .ConfigureAwait(false);

        AssertChannel(dispatched, NotificationChannel.InApp, inApp);
        AssertChannel(dispatched, NotificationChannel.Email, email);
        AssertChannel(dispatched, NotificationChannel.Sms, sms);
        Assert.Equal(email ? 1 : 0, emailLog.Sent.Count);
        Assert.Equal(sms ? 1 : 0, smsLog.Sent.Count);
    }

    // Feature: elearning-app-zimbabwe, Property 41: SMS Retry Logic — Validates: Requirements 15.5
    [Property(MaxTest = 100)]
    public async Task Property41_failed_sms_retries_increment_until_undelivered(byte attemptsRaw)
    {
        int attempts = (attemptsRaw % 8) + 1;
        using ServiceProvider provider = NotificationPropertyTestHost.Create(smsSucceeds: false);
        using IServiceScope scope = provider.CreateScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid notificationId = await NotificationPropertySeeds
            .SeedFailedSmsScenarioAsync(db, tenantId, userId)
            .ConfigureAwait(false);

        for (int i = 0; i < attempts; i++)
        {
            await mediator.Send(new RetryFailedSmsCommand(tenantId, notificationId), CancellationToken.None)
                .ConfigureAwait(false);
        }

        Notification row = await db.Notifications.AsNoTracking()
            .SingleAsync(n => n.Id == notificationId)
            .ConfigureAwait(false);

        int expectedRetries = Math.Min(attempts, SmsRetryRules.MaxRetryAttempts);
        Assert.Equal(expectedRetries, row.RetryCount);
        Assert.True(row.RetryCount <= SmsRetryRules.MaxRetryAttempts);
        Assert.Equal(
            attempts >= SmsRetryRules.MaxRetryAttempts ? NotificationStatus.Undelivered : NotificationStatus.Failed,
            row.Status);
    }

    private static void AssertChannel(
        List<NotificationChannel> dispatched,
        NotificationChannel channel,
        bool enabled)
    {
        if (enabled)
            Assert.Contains(channel, dispatched);
        else
            Assert.DoesNotContain(channel, dispatched);
    }
}
