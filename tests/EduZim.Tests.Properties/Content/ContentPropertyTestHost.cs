using EduZim.Application;
using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Gamification;
using EduZim.Tests.Properties.Tenants;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Content;

internal static class ContentPropertyTestHost
{
    public static ServiceProvider Create(RecordingContentBackgroundJobs? recordingJobs = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();

        var dbName = "ContentProp-" + Guid.NewGuid();
        services.AddDbContext<EduZimDbContext>(o => o.UseInMemoryDatabase(dbName));

        services.AddApplication();

        services.Configure<ContentStorageOptions>(_ =>
        {
            _.MaxVideoBytes = 500L * 1024 * 1024;
            _.MaxAudioBytes = 50L * 1024 * 1024;
            _.MaxOtherBytes = 100L * 1024 * 1024;
            _.ArchivedRetentionDays = 30;
            _.SignedUrlExpiryMinutes = 60;
        });

        var jobs = recordingJobs ?? new RecordingContentBackgroundJobs();
        services.AddSingleton(_ => jobs);
        services.AddSingleton<IContentBackgroundJobs>(sp => sp.GetRequiredService<RecordingContentBackgroundJobs>());
        services.AddSingleton<IGamificationBackgroundJobs, NoOpGamificationBackgroundJobs>();

        services.AddSingleton<FakeStorageService>();
        services.AddSingleton<IStorageService>(sp => sp.GetRequiredService<FakeStorageService>());

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

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
            ctx.Database.EnsureCreated();
            SeedRolesAsync(scope.ServiceProvider).GetAwaiter().GetResult();
        }

        return provider;
    }

    private static async Task SeedRolesAsync(IServiceProvider sp)
    {
        var roles = new[] { "PlatformAdmin", "SchoolAdmin", "Student", "ParentGuardian", "Teacher" };
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var name in roles)
        {
            if (!await roleManager.RoleExistsAsync(name).ConfigureAwait(false))
                await roleManager.CreateAsync(new IdentityRole<Guid>(name)).ConfigureAwait(false);
        }
    }
}
