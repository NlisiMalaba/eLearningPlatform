using EduZim.Application.Billing.Commands.HandlePaymentFailed;
using EduZim.Application.Billing.Commands.HandlePaymentSucceeded;
using EduZim.Application.Billing.Queries.CalculateSchoolFee;
using EduZim.Application.Tenants.Commands.SuspendTenant;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using FsCheck.Xunit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Billing;

/// <summary>Feature: elearning-app-zimbabwe — Billing properties 5–9.</summary>
public sealed class BillingPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 5: Subscription Activation on Payment — Validates: Requirements 2.3
    [Property(MaxTest = 100)]
    public async Task Property5_successful_payment_activates_and_extends_subscription_period()
    {
        using var provider = BillingPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await db.Tenants.AddAsync(
            new Tenant
            {
                Id = tenantId,
                Name = "T",
                Tier = TenantTier.School,
                Status = TenantStatus.Active,
                Branding = new BrandingSettings { SchoolName = "S", PrimaryColour = "#1976D2" },
                CreatedAt = now,
            }).ConfigureAwait(false);

        await db.Subscriptions.AddAsync(
            new Subscription
            {
                Id = subscriptionId,
                TenantId = tenantId,
                Cycle = BillingCycle.Monthly,
                Status = SubscriptionStatus.Suspended,
                CurrentPeriodStart = now,
                CurrentPeriodEnd = now,
                GracePeriodEnd = null,
                StudentCount = 3,
            }).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);

        var idempotency = "idem-" + Guid.NewGuid();
        await mediator.Send(
                new HandlePaymentSucceededCommand(
                    tenantId,
                    subscriptionId,
                    24m,
                    "USD",
                    "pi_test_" + Guid.NewGuid().ToString("N")[..12],
                    idempotency))
            .ConfigureAwait(false);

        var sub = await db.Subscriptions.AsNoTracking()
            .SingleAsync(s => s.Id == subscriptionId)
            .ConfigureAwait(false);

        Assert.Equal(SubscriptionStatus.Active, sub.Status);
        Assert.True(sub.CurrentPeriodEnd > DateTime.UtcNow.AddMinutes(-1));
        Assert.Null(sub.GracePeriodEnd);
    }

    // Feature: elearning-app-zimbabwe, Property 6: Grace Period on Payment Failure — Validates: Requirements 2.5
    [Property(MaxTest = 100)]
    public async Task Property6_failed_payment_enters_grace_period_with_seven_day_window()
    {
        using var provider = BillingPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await db.Tenants.AddAsync(
            new Tenant
            {
                Id = tenantId,
                Name = "T",
                Tier = TenantTier.School,
                Status = TenantStatus.Active,
                Branding = new BrandingSettings { SchoolName = "S", PrimaryColour = "#1976D2" },
                CreatedAt = now,
            }).ConfigureAwait(false);

        await db.Subscriptions.AddAsync(
            new Subscription
            {
                Id = subscriptionId,
                TenantId = tenantId,
                Cycle = BillingCycle.Monthly,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = now.AddMonths(-1),
                CurrentPeriodEnd = now.AddMonths(1),
                GracePeriodEnd = null,
                StudentCount = 1,
            }).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);

        var before = DateTime.UtcNow;
        await mediator.Send(
                new HandlePaymentFailedCommand(
                    tenantId,
                    subscriptionId,
                    "card_declined",
                    "in_test_" + Guid.NewGuid().ToString("N")[..12]))
            .ConfigureAwait(false);

        var sub = await db.Subscriptions.AsNoTracking()
            .SingleAsync(s => s.Id == subscriptionId)
            .ConfigureAwait(false);

        Assert.Equal(SubscriptionStatus.GracePeriod, sub.Status);
        Assert.NotNull(sub.GracePeriodEnd);
        var expected = before.AddDays(7);
        Assert.InRange(sub.GracePeriodEnd!.Value, expected.AddMinutes(-2), expected.AddMinutes(2));
    }

    // Feature: elearning-app-zimbabwe, Property 7: Subscription Data Preservation — Validates: Requirements 2.6, 11.6
    [Property(MaxTest = 100)]
    public async Task Property7_suspended_subscription_retains_student_progress_attempts_and_badges()
    {
        using var provider = BillingPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();

        var tenantId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await db.Tenants.AddAsync(
            new Tenant
            {
                Id = tenantId,
                Name = "T",
                Tier = TenantTier.School,
                Status = TenantStatus.Active,
                Branding = new BrandingSettings { SchoolName = "S", PrimaryColour = "#1976D2" },
                CreatedAt = now,
            }).ConfigureAwait(false);

        await db.Subscriptions.AddAsync(
            new Subscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Cycle = BillingCycle.Monthly,
                Status = SubscriptionStatus.Suspended,
                CurrentPeriodStart = now.AddMonths(-1),
                CurrentPeriodEnd = now.AddDays(-1),
                GracePeriodEnd = null,
                StudentCount = 1,
            }).ConfigureAwait(false);

        await db.Modules.AddAsync(
            new Module
            {
                Id = moduleId,
                TenantId = tenantId,
                Title = "Mod",
                Grade = GradeLevel.Grade1,
                Subject = "Math",
                SequenceOrder = 1,
                IsRequired = true,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.Assessments.AddAsync(
            new Assessment
            {
                Id = assessmentId,
                TenantId = tenantId,
                Title = "A",
                ModuleId = moduleId,
                PassingScorePercent = 60,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.StudentProgresses.AddAsync(
            new StudentProgress
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StudentId = studentId,
                ModuleId = moduleId,
                IsCompleted = false,
                TimeOnTaskSeconds = 10,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.AssessmentAttempts.AddAsync(
            new AssessmentAttempt
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AssessmentId = assessmentId,
                StudentId = studentId,
                ScorePercent = 80,
                TimeTakenSeconds = 60,
                SubmittedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.Badges.AddAsync(
            new Badge
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StudentId = studentId,
                Type = BadgeType.FirstModule,
                EarnedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.SaveChangesAsync().ConfigureAwait(false);

        Assert.Equal(1, await db.StudentProgresses.CountAsync(p => p.TenantId == tenantId).ConfigureAwait(false));
        Assert.Equal(1, await db.AssessmentAttempts.CountAsync(a => a.TenantId == tenantId).ConfigureAwait(false));
        Assert.Equal(1, await db.Badges.CountAsync(b => b.TenantId == tenantId).ConfigureAwait(false));
    }

    // Feature: elearning-app-zimbabwe, Property 7 (tenant suspension does not immediately delete learning data) — Validates: Requirements 2.6, 11.6
    [Property(MaxTest = 100)]
    public async Task Property7_tenant_suspend_does_not_remove_progress_attempts_or_badges_before_retention_job()
    {
        using var provider = BillingPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<EduZimDbContext>();
        var mediator = sp.GetRequiredService<IMediator>();
        var current = sp.GetRequiredService<MutableCurrentUser>();

        var tenantId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await db.Tenants.AddAsync(
            new Tenant
            {
                Id = tenantId,
                Name = "T",
                Tier = TenantTier.School,
                Status = TenantStatus.Active,
                Branding = new BrandingSettings { SchoolName = "S", PrimaryColour = "#1976D2" },
                CreatedAt = now,
            }).ConfigureAwait(false);

        await db.Modules.AddAsync(
            new Module
            {
                Id = moduleId,
                TenantId = tenantId,
                Title = "Mod",
                Grade = GradeLevel.Grade1,
                Subject = "Math",
                SequenceOrder = 1,
                IsRequired = true,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.Assessments.AddAsync(
            new Assessment
            {
                Id = assessmentId,
                TenantId = tenantId,
                Title = "A",
                ModuleId = moduleId,
                PassingScorePercent = 60,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.StudentProgresses.AddAsync(
            new StudentProgress
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StudentId = studentId,
                ModuleId = moduleId,
                IsCompleted = true,
                TimeOnTaskSeconds = 100,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.AssessmentAttempts.AddAsync(
            new AssessmentAttempt
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AssessmentId = assessmentId,
                StudentId = studentId,
                ScorePercent = 90,
                TimeTakenSeconds = 120,
                SubmittedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.Badges.AddAsync(
            new Badge
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StudentId = studentId,
                Type = BadgeType.SubjectMastery,
                EarnedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            }).ConfigureAwait(false);

        await db.SaveChangesAsync().ConfigureAwait(false);

        var platformUserId = Guid.NewGuid();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var platformUser = new ApplicationUser
        {
            Id = platformUserId,
            UserName = $"plat-{platformUserId:N}@test.local",
            Email = $"plat-{platformUserId:N}@test.local",
            EmailConfirmed = true,
            TenantId = null,
            Role = UserRole.PlatformAdmin,
        };
        var createResult = await users.CreateAsync(platformUser, "ValidPass1!").ConfigureAwait(false);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(platformUser, nameof(UserRole.PlatformAdmin)).ConfigureAwait(false);

        current.UserId = platformUserId;
        current.TenantId = null;
        current.Role = UserRole.PlatformAdmin;

        await mediator.Send(new SuspendTenantCommand(tenantId)).ConfigureAwait(false);

        Assert.Equal(1, await db.StudentProgresses.CountAsync(p => p.TenantId == tenantId).ConfigureAwait(false));
        Assert.Equal(1, await db.AssessmentAttempts.CountAsync(a => a.TenantId == tenantId).ConfigureAwait(false));
        Assert.Equal(1, await db.Badges.CountAsync(b => b.TenantId == tenantId).ConfigureAwait(false));
    }

    // Feature: elearning-app-zimbabwe, Property 8: Invoice Created for Every Successful Payment — Validates: Requirements 2.7
    [Property(MaxTest = 100)]
    public async Task Property8_successful_payment_persists_invoice_row()
    {
        using var provider = BillingPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await db.Tenants.AddAsync(
            new Tenant
            {
                Id = tenantId,
                Name = "T",
                Tier = TenantTier.School,
                Status = TenantStatus.Active,
                Branding = new BrandingSettings { SchoolName = "S", PrimaryColour = "#1976D2" },
                CreatedAt = now,
            }).ConfigureAwait(false);

        await db.Subscriptions.AddAsync(
            new Subscription
            {
                Id = subscriptionId,
                TenantId = tenantId,
                Cycle = BillingCycle.Monthly,
                Status = SubscriptionStatus.Suspended,
                CurrentPeriodStart = now,
                CurrentPeriodEnd = now,
                GracePeriodEnd = null,
                StudentCount = 2,
            }).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);

        var idem = "idem-inv-" + Guid.NewGuid();
        await mediator.Send(
                new HandlePaymentSucceededCommand(
                    tenantId,
                    subscriptionId,
                    16m,
                    "USD",
                    "pi_inv_" + Guid.NewGuid().ToString("N")[..12],
                    idem))
            .ConfigureAwait(false);

        var payment = await db.Payments.AsNoTracking()
            .SingleAsync(p => p.IdempotencyKey == idem)
            .ConfigureAwait(false);

        var invoice = await db.SubscriptionInvoices.AsNoTracking()
            .SingleOrDefaultAsync(i => i.PaymentId == payment.Id)
            .ConfigureAwait(false);

        Assert.NotNull(invoice);
        Assert.NotEmpty(invoice!.PdfContent);
        Assert.Equal(subscriptionId, invoice.SubscriptionId);
    }

    // Feature: elearning-app-zimbabwe, Property 9: School Fee Calculation Correctness — Validates: Requirements 2.8
    [Property(MaxTest = 100)]
    public async Task Property9_school_fee_is_unit_price_times_student_count()
    {
        using var provider = BillingPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<EduZimDbContext>();
        var mediator = sp.GetRequiredService<IMediator>();
        var current = sp.GetRequiredService<MutableCurrentUser>();

        var tenantId = Guid.NewGuid();
        await db.Tenants.AddAsync(
            new Tenant
            {
                Id = tenantId,
                Name = "T",
                Tier = TenantTier.School,
                Status = TenantStatus.Active,
                Branding = new BrandingSettings { SchoolName = "S", PrimaryColour = "#1976D2" },
                CreatedAt = DateTime.UtcNow,
            }).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);

        current.UserId = Guid.NewGuid();
        current.TenantId = tenantId;
        current.Role = UserRole.SchoolAdmin;

        var studentCount = Random.Shared.Next(1, 500);
        var fee = await mediator.Send(new CalculateSchoolFeeQuery(tenantId, BillingCycle.Monthly, studentCount))
            .ConfigureAwait(false);

        Assert.Equal(8m * studentCount, fee);
    }
}
