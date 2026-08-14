using EduZim.Application;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Assessments;
using EduZim.Tests.Properties.Tenants;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Gamification;

internal static class GamificationPropertyTestHost
{
    public static ServiceProvider Create()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDataProtection();

        string dbName = "GamificationProp-" + Guid.NewGuid();
        services.AddDbContext<EduZimDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddApplication();

        services.AddSingleton<RecordingGamificationBackgroundJobs>();
        services.AddSingleton<IGamificationBackgroundJobs>(
            sp => sp.GetRequiredService<RecordingGamificationBackgroundJobs>());
        services.AddSingleton<ICacheService, EphemeralCacheService>();
        services.AddSingleton<ITenantBackgroundJobs, NoOpTenantBackgroundJobs>();
        services.AddSingleton<MutableCurrentUser>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<MutableCurrentUser>());
        services.AddScoped<IEduZimDbContext>(sp => sp.GetRequiredService<EduZimDbContext>());

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.Lockout.AllowedForNewUsers = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<EduZimDbContext>()
            .AddDefaultTokenProviders();

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        EduZimDbContext ctx = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        ctx.Database.EnsureCreated();
        return provider;
    }

    public static async Task SeedStartingPointsAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        int totalPoints)
    {
        if (totalPoints <= 0)
            return;

        DateTime utcNow = DateTime.UtcNow;
        await db.StudentPoints.AddAsync(
                new StudentPoints
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StudentId = studentId,
                    TotalPoints = totalPoints,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task SeedModuleAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid moduleId,
        string subject,
        GradeLevel grade)
    {
        DateTime utcNow = DateTime.UtcNow;
        await db.Modules.AddAsync(
                new Module
                {
                    Id = moduleId,
                    TenantId = tenantId,
                    Title = subject,
                    Subject = subject,
                    Grade = grade,
                    SequenceOrder = 1,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task SeedCompletedProgressAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Guid moduleId,
        DateTime completedAt)
    {
        DateTime utcNow = DateTime.UtcNow;
        await db.StudentProgresses.AddAsync(
                new StudentProgress
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StudentId = studentId,
                    ModuleId = moduleId,
                    IsCompleted = true,
                    CompletedAt = completedAt,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task SeedSubmittedAttemptAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        DateTime submittedAt)
    {
        DateTime utcNow = DateTime.UtcNow;
        await db.AssessmentAttempts.AddAsync(
                new AssessmentAttempt
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AssessmentId = Guid.NewGuid(),
                    StudentId = studentId,
                    StartedAt = submittedAt.AddMinutes(-10),
                    ScorePercent = 70,
                    TimeTakenSeconds = 60,
                    SubmittedAt = submittedAt,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task SeedPointsRowAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        int totalPoints)
    {
        DateTime utcNow = DateTime.UtcNow;
        await db.StudentPoints.AddAsync(
                new StudentPoints
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StudentId = studentId,
                    TotalPoints = totalPoints,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
