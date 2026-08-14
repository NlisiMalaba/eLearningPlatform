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

namespace EduZim.Tests.Properties.LiveClassrooms;

internal static class LiveClassroomPropertyTestHost
{
    public static ServiceProvider Create()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDataProtection();

        string dbName = "LiveClassroomProp-" + Guid.NewGuid();
        services.AddDbContext<EduZimDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddApplication();

        services.AddSingleton<RecordingEmailService>();
        services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<RecordingEmailService>());
        services.AddSingleton<RecordingSmsService>();
        services.AddSingleton<ISmsService>(sp => sp.GetRequiredService<RecordingSmsService>());
        services.AddSingleton<RecordingNotificationBackgroundJobs>();
        services.AddSingleton<INotificationBackgroundJobs>(
            sp => sp.GetRequiredService<RecordingNotificationBackgroundJobs>());
        services.AddSingleton<IVideoService, StubVideoService>();
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
        return provider;
    }

    public static IServiceScope CreateScope(ServiceProvider provider)
    {
        IServiceScope scope = provider.CreateScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        db.Database.EnsureCreated();
        return scope;
    }
}

internal sealed class StubVideoService : IVideoService
{
    public Task<string> GetJoinTokenAsync(string roomId, string participantId, CancellationToken ct = default) =>
        Task.FromResult($"token:{roomId}:{participantId}");

    public Task<string?> GetRecordingUrlAsync(string roomId, CancellationToken ct = default) =>
        Task.FromResult<string?>($"https://recordings.test/{roomId}");
}
