using EduZim.Application;
using EduZim.Application.Billing.Services;
using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Billing;

internal static class BillingPropertyTestHost
{
    public static ServiceProvider Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();

        var dbName = "BillingProp-" + Guid.NewGuid();
        services.AddDbContext<EduZimDbContext>(o => o.UseInMemoryDatabase(dbName));

        services.AddApplication();

        services.Configure<BillingPricingOptions>(_ =>
        {
            _.PreSchoolMonthly = 5m;
            _.PreSchoolYearly = 50m;
            _.SchoolMonthly = 8m;
            _.SchoolTermly = 22m;
            _.SchoolYearly = 75m;
            _.TermlyPeriodMonths = 4;
            _.RenewalReminderUtcHour = 8;
        });

        services.Configure<TenantLifecycleSettings>(_ =>
        {
            _.InviteCodeLifetimeDays = 7;
            _.SuspendedTenantRetentionDays = 90;
        });

        services.AddSingleton<MutableCurrentUser>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<MutableCurrentUser>());
        services.AddSingleton<ITenantBackgroundJobs, NoOpTenantBackgroundJobs>();
        services.AddScoped<IEduZimDbContext>(sp => sp.GetRequiredService<EduZimDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBillingPricingService, BillingPricingService>();
        services.AddScoped<IBillingPeriodService, BillingPeriodService>();
        services.AddSingleton<IInvoicePdfGenerator, FakeInvoicePdfGenerator>();
        services.AddScoped<IBillingInvoiceService, BillingInvoiceService>();

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
        var rm = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var name in roles)
        {
            if (!await rm.RoleExistsAsync(name).ConfigureAwait(false))
            {
                await rm.CreateAsync(
                    new IdentityRole<Guid>
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        NormalizedName = name.ToUpperInvariant(),
                    }).ConfigureAwait(false);
            }
        }
    }
}
