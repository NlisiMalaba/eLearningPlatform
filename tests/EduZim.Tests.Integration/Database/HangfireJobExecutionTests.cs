using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Jobs;
using EduZim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Integration.Database;

[Collection(PostgresRlsCollection.Name)]
public sealed class HangfireJobExecutionTests
{
    private readonly PostgresRlsFixture _fixture;

    public HangfireJobExecutionTests(PostgresRlsFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Content_permanent_deletion_job_removes_archived_content_and_leaves_live_content()
    {
        _fixture.EnsureDockerAvailable();

        Guid archivedId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        await using (AsyncServiceScope seedScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            EduZimDbContext db = seedScope.ServiceProvider.GetRequiredService<EduZimDbContext>();
            await db.SetSessionTenantIdAsync(_fixture.TenantAId);
            db.ContentItems.Add(new ContentItem
            {
                Id = archivedId,
                TenantId = _fixture.TenantAId,
                Title = "Archived for cleanup",
                Type = ContentType.Pdf,
                StorageKey = $"content/{archivedId:N}.pdf",
                FileSizeBytes = 64,
                Language = "en",
                Status = ContentStatus.Archived,
                ArchivedAt = now.AddDays(-31),
                UploadedByUserId = _fixture.TenantAAdmin.Id,
                CreatedAt = now.AddDays(-40),
                UpdatedAt = now.AddDays(-31),
            });
            await db.SaveChangesAsync();
        }

        await using (AsyncServiceScope jobScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ContentPermanentDeletionJob job =
                jobScope.ServiceProvider.GetRequiredService<ContentPermanentDeletionJob>();
            await job.RunAsync(_fixture.TenantAId, archivedId);
            await job.RunAsync(_fixture.TenantAId, _fixture.ContentAId);
        }

        await using AsyncServiceScope assertScope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await assertDb.SetSessionTenantIdAsync(_fixture.TenantAId);

        Assert.False(await assertDb.ContentItems.AnyAsync(c => c.Id == archivedId));
        Assert.True(await assertDb.ContentItems.AnyAsync(c => c.Id == _fixture.ContentAId));
    }

    [SkippableFact]
    public async Task Renewal_reminder_job_marks_due_subscription_once()
    {
        _fixture.EnsureDockerAvailable();

        Guid subscriptionId = Guid.NewGuid();
        DateTime periodEnd = DateTime.UtcNow.AddDays(5);
        await using (AsyncServiceScope seedScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            EduZimDbContext db = seedScope.ServiceProvider.GetRequiredService<EduZimDbContext>();
            await db.SetSessionTenantIdAsync(_fixture.TenantAId);
            db.Subscriptions.Add(new Subscription
            {
                Id = subscriptionId,
                TenantId = _fixture.TenantAId,
                Cycle = BillingCycle.Monthly,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = periodEnd.AddMonths(-1),
                CurrentPeriodEnd = periodEnd,
                StudentCount = 2,
            });
            await db.SaveChangesAsync();
        }

        await using (AsyncServiceScope jobScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            SubscriptionRenewalReminderJob job =
                jobScope.ServiceProvider.GetRequiredService<SubscriptionRenewalReminderJob>();
            await job.RunAsync();
            await job.RunAsync();
        }

        await using AsyncServiceScope assertScope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await assertDb.SetSessionTenantIdAsync(_fixture.TenantAId);
        Subscription subscription = await assertDb.Subscriptions.AsNoTracking()
            .SingleAsync(s => s.Id == subscriptionId);

        Assert.Equal(periodEnd, subscription.CurrentPeriodEnd, TimeSpan.FromSeconds(1));
        Assert.Equal(subscription.CurrentPeriodEnd, subscription.RenewalReminderSentForPeriodEndUtc);
    }

    [SkippableFact]
    public async Task Inactivity_alert_job_notifies_linked_parent_and_records_alert_timestamp()
    {
        _fixture.EnsureDockerAvailable();

        Guid studentId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        DateTime lastLogin = DateTime.UtcNow.AddDays(-8);
        DateTime now = DateTime.UtcNow;

        await using (AsyncServiceScope seedScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            EduZimDbContext db = seedScope.ServiceProvider.GetRequiredService<EduZimDbContext>();
            await db.SetSessionTenantIdAsync(_fixture.TenantAId);
            db.Users.AddRange(
                CreateUser(studentId, UserRole.Student, "inactive-student", lastLogin),
                CreateUser(parentId, UserRole.ParentGuardian, "linked-parent", lastLoginAt: null));
            db.ParentStudentLinks.Add(new ParentStudentLink
            {
                Id = Guid.NewGuid(),
                TenantId = _fixture.TenantAId,
                ParentUserId = parentId,
                StudentUserId = studentId,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await db.SaveChangesAsync();
        }

        await using (AsyncServiceScope jobScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            StudentInactivityAlertJob job =
                jobScope.ServiceProvider.GetRequiredService<StudentInactivityAlertJob>();
            await job.RunAsync();
        }

        await using AsyncServiceScope assertScope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await assertDb.SetSessionTenantIdAsync(_fixture.TenantAId);

        ApplicationUser student = await assertDb.Users.AsNoTracking().SingleAsync(u => u.Id == studentId);
        Assert.NotNull(student.LastInactivityAlertAt);
        Assert.True(student.LastInactivityAlertAt >= lastLogin);

        List<Notification> alerts = await assertDb.Notifications.AsNoTracking()
            .Where(n => n.UserId == parentId && n.Type == NotificationType.InactivityAlert)
            .ToListAsync();
        Assert.NotEmpty(alerts);
        Assert.All(alerts, n => Assert.Equal(_fixture.TenantAId, n.TenantId));
    }

    private ApplicationUser CreateUser(
        Guid id,
        UserRole role,
        string label,
        DateTime? lastLoginAt)
    {
        string email = $"{label}-{id:N}@eduzim.test";
        return new ApplicationUser
        {
            Id = id,
            TenantId = _fixture.TenantAId,
            Role = role,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = label,
            NormalizedUserName = email.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            LastLoginAt = lastLoginAt,
        };
    }
}
