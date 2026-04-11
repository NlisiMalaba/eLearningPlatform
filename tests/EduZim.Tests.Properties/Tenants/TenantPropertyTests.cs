using EduZim.Application.Exceptions;
using EduZim.Application.Tenants.Commands.GenerateInviteCode;
using EduZim.Application.Tenants.Commands.RedeemInviteCode;
using EduZim.Application.Tenants.Commands.SuspendTenant;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using FsCheck.Xunit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Tenants;

/// <summary>
/// Feature: elearning-app-zimbabwe — Tenant properties 7 and 30.
/// </summary>
public sealed class TenantPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 30: Parent Invite Code Round Trip — Validates: Requirements 10.3
    [Property(MaxTest = 100)]
    public async Task Property30_valid_invite_code_redeem_creates_parent_student_link()
    {
        using var provider = TenantPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<EduZimDbContext>();
        var mediator = sp.GetRequiredService<IMediator>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var current = sp.GetRequiredService<MutableCurrentUser>();

        var tenantId = Guid.NewGuid();
        await db.Tenants.AddAsync(
            new Tenant
            {
                Id = tenantId,
                Name = "School " + tenantId.ToString("N")[..8],
                Tier = TenantTier.School,
                Status = TenantStatus.Active,
                Branding = new BrandingSettings { SchoolName = "School", PrimaryColour = "#1976D2" },
                CreatedAt = DateTime.UtcNow,
            }).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);

        var studentId = Guid.NewGuid();
        var student = new ApplicationUser
        {
            Id = studentId,
            UserName = $"stu-{studentId:N}@test.local",
            Email = $"stu-{studentId:N}@test.local",
            EmailConfirmed = true,
            TenantId = tenantId,
            Role = UserRole.Student,
        };
        AssertIdentity(await users.CreateAsync(student, "ValidPass1!").ConfigureAwait(false));
        await users.AddToRoleAsync(student, nameof(UserRole.Student)).ConfigureAwait(false);

        var adminId = Guid.NewGuid();
        var admin = new ApplicationUser
        {
            Id = adminId,
            UserName = $"adm-{adminId:N}@test.local",
            Email = $"adm-{adminId:N}@test.local",
            EmailConfirmed = true,
            TenantId = tenantId,
            Role = UserRole.SchoolAdmin,
        };
        AssertIdentity(await users.CreateAsync(admin, "ValidPass1!").ConfigureAwait(false));
        await users.AddToRoleAsync(admin, nameof(UserRole.SchoolAdmin)).ConfigureAwait(false);

        current.UserId = adminId;
        current.TenantId = tenantId;
        current.Role = UserRole.SchoolAdmin;

        var generated = await mediator
            .Send(new GenerateInviteCodeCommand(tenantId, studentId))
            .ConfigureAwait(false);

        var parentId = Guid.NewGuid();
        var parent = new ApplicationUser
        {
            Id = parentId,
            UserName = $"par-{parentId:N}@test.local",
            Email = $"par-{parentId:N}@test.local",
            EmailConfirmed = true,
            TenantId = null,
            Role = UserRole.ParentGuardian,
        };
        AssertIdentity(await users.CreateAsync(parent, "ValidPass1!").ConfigureAwait(false));
        await users.AddToRoleAsync(parent, nameof(UserRole.ParentGuardian)).ConfigureAwait(false);

        current.UserId = parentId;
        current.TenantId = null;
        current.Role = UserRole.ParentGuardian;

        await mediator.Send(new RedeemInviteCodeCommand(generated.Code)).ConfigureAwait(false);

        var link = await db.ParentStudentLinks.AsNoTracking()
            .SingleOrDefaultAsync(
                l => l.TenantId == tenantId && l.ParentUserId == parentId && l.StudentUserId == studentId)
            .ConfigureAwait(false);
        Assert.NotNull(link);

        var invite = await db.TenantInviteCodes.AsNoTracking()
            .SingleAsync(c => c.Code == generated.Code)
            .ConfigureAwait(false);
        Assert.NotNull(invite.UsedAtUtc);
        Assert.Equal(parentId, invite.UsedByParentUserId);
    }

    // Feature: elearning-app-zimbabwe, Property 30 (invalid code) — Validates: Requirements 10.3
    [Property(MaxTest = 50)]
    public async Task Property30_unknown_invite_code_redeem_fails()
    {
        using var provider = TenantPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var current = scope.ServiceProvider.GetRequiredService<MutableCurrentUser>();

        var parentId = Guid.NewGuid();
        var parent = new ApplicationUser
        {
            Id = parentId,
            UserName = $"p-{parentId:N}@test.local",
            Email = $"p-{parentId:N}@test.local",
            EmailConfirmed = true,
            TenantId = null,
            Role = UserRole.ParentGuardian,
        };
        AssertIdentity(await users.CreateAsync(parent, "ValidPass1!").ConfigureAwait(false));
        await users.AddToRoleAsync(parent, nameof(UserRole.ParentGuardian)).ConfigureAwait(false);

        current.UserId = parentId;
        current.Role = UserRole.ParentGuardian;

        await Assert.ThrowsAsync<NotFoundException>(
            () => mediator.Send(new RedeemInviteCodeCommand("NO_SUCH_CODE_" + Guid.NewGuid().ToString("N")[..8])));
    }

    // Feature: elearning-app-zimbabwe, Property 30 (expired code) — Validates: Requirements 10.3
    [Property(MaxTest = 50)]
    public async Task Property30_expired_invite_code_redeem_fails()
    {
        using var provider = TenantPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<EduZimDbContext>();
        var mediator = sp.GetRequiredService<IMediator>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
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

        var studentId = Guid.NewGuid();
        await db.TenantInviteCodes.AddAsync(
            new TenantInviteCode
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StudentUserId = studentId,
                Code = "EXPIRED" + Guid.NewGuid().ToString("N")[..8],
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-2),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
            }).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);

        var parentId = Guid.NewGuid();
        var parent = new ApplicationUser
        {
            Id = parentId,
            UserName = $"px-{parentId:N}@test.local",
            Email = $"px-{parentId:N}@test.local",
            EmailConfirmed = true,
            TenantId = null,
            Role = UserRole.ParentGuardian,
        };
        AssertIdentity(await users.CreateAsync(parent, "ValidPass1!").ConfigureAwait(false));
        await users.AddToRoleAsync(parent, nameof(UserRole.ParentGuardian)).ConfigureAwait(false);

        current.UserId = parentId;
        current.Role = UserRole.ParentGuardian;

        var expiredCode = await db.TenantInviteCodes.SingleAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(new RedeemInviteCodeCommand(expiredCode.Code)));
    }

    // Feature: elearning-app-zimbabwe, Property 7: Subscription Data Preservation — Validates: Requirements 2.6, 11.6
    [Property(MaxTest = 100)]
    public async Task Property7_suspended_subscription_retains_student_progress_attempts_and_badges()
    {
        using var provider = TenantPropertyTestHost.Create();
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

    // Feature: elearning-app-zimbabwe, Property 7 (tenant suspension does not immediately delete learning data)
    [Property(MaxTest = 50)]
    public async Task Property7_tenant_suspend_does_not_remove_progress_attempts_or_badges_before_retention_job()
    {
        using var provider = TenantPropertyTestHost.Create();
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

        current.UserId = Guid.NewGuid();
        current.TenantId = null;
        current.Role = UserRole.PlatformAdmin;

        await mediator.Send(new SuspendTenantCommand(tenantId)).ConfigureAwait(false);

        Assert.Equal(1, await db.StudentProgresses.CountAsync(p => p.TenantId == tenantId).ConfigureAwait(false));
        Assert.Equal(1, await db.AssessmentAttempts.CountAsync(a => a.TenantId == tenantId).ConfigureAwait(false));
        Assert.Equal(1, await db.Badges.CountAsync(b => b.TenantId == tenantId).ConfigureAwait(false));
    }

    private static void AssertIdentity(IdentityResult result)
    {
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
