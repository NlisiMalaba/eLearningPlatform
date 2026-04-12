using EduZim.Application;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Assessments;

internal static class AssessmentPropertyTestHost
{
    public static ServiceProvider Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();

        string dbName = "AssessmentProp-" + Guid.NewGuid();
        services.AddDbContext<EduZimDbContext>(o => o.UseInMemoryDatabase(dbName));

        services.AddApplication();

        services.AddSingleton<NoOpAssessmentBackgroundJobs>();
        services.AddSingleton<IAssessmentBackgroundJobs>(sp => sp.GetRequiredService<NoOpAssessmentBackgroundJobs>());
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

        using (IServiceScope scope = provider.CreateScope())
        {
            EduZimDbContext ctx = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
            ctx.Database.EnsureCreated();
            SeedRolesAsync(scope.ServiceProvider).GetAwaiter().GetResult();
        }

        return provider;
    }

    internal static async Task SeedTenantModuleClassAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid moduleId,
        Guid classId,
        DateTime now)
    {
        await db.Tenants.AddAsync(
                new Tenant
                {
                    Id = tenantId,
                    Name = "T",
                    Tier = TenantTier.School,
                    Status = TenantStatus.Active,
                    Branding = new BrandingSettings { SchoolName = "S", PrimaryColour = "#1976D2" },
                    CreatedAt = now,
                })
            .ConfigureAwait(false);

        await db.Modules.AddAsync(
                new Module
                {
                    Id = moduleId,
                    TenantId = tenantId,
                    Title = "Mod",
                    Grade = GradeLevel.Grade1,
                    Subject = "Subj",
                    SequenceOrder = 1,
                    IsRequired = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                })
            .ConfigureAwait(false);

        await db.SchoolClasses.AddAsync(
                new SchoolClass
                {
                    Id = classId,
                    TenantId = tenantId,
                    Name = "C",
                    CreatedAt = now,
                    UpdatedAt = now,
                })
            .ConfigureAwait(false);

        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private static async Task SeedRolesAsync(IServiceProvider sp)
    {
        string[] roles = ["PlatformAdmin", "SchoolAdmin", "Student", "ParentGuardian", "Teacher"];
        RoleManager<IdentityRole<Guid>> roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (string name in roles)
        {
            if (!await roleManager.RoleExistsAsync(name).ConfigureAwait(false))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(name)).ConfigureAwait(false);
            }
        }
    }
}
