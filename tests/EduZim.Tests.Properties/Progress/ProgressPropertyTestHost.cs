using EduZim.Application;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Assessments;
using EduZim.Tests.Properties.Gamification;
using EduZim.Tests.Properties.Notifications;
using EduZim.Tests.Properties.Tenants;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Progress;

internal static class ProgressPropertyTestHost
{
    public static ServiceProvider Create()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDataProtection();

        string dbName = "ProgressProp-" + Guid.NewGuid();
        services.AddDbContext<EduZimDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddApplication();

        services.AddSingleton<RecordingEmailService>();
        services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<RecordingEmailService>());
        services.AddSingleton<RecordingSmsService>();
        services.AddSingleton<ISmsService>(sp => sp.GetRequiredService<RecordingSmsService>());
        services.AddSingleton<RecordingNotificationBackgroundJobs>();
        services.AddSingleton<INotificationBackgroundJobs>(
            sp => sp.GetRequiredService<RecordingNotificationBackgroundJobs>());
        services.AddSingleton<IGamificationBackgroundJobs, NoOpGamificationBackgroundJobs>();
        services.AddSingleton<ITenantBackgroundJobs, NoOpTenantBackgroundJobs>();
        services.AddSingleton<ICacheService, EphemeralCacheService>();
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
}
