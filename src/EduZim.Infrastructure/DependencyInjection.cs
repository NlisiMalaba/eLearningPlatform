using System.Text;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Infrastructure.Audit;
using EduZim.Infrastructure.Caching;
using EduZim.Infrastructure.Identity;
using EduZim.Infrastructure.Persistence;
using EduZim.Infrastructure.Persistence.Interceptors;
using EduZim.Infrastructure.Tenants;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EduZim.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection();
        services.AddSingleton<TenantConnectionInterceptor>();

        var redisConnection = configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(redisConnection))
            services.AddDistributedMemoryCache();
        else
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        services.AddSingleton<ICacheService, DistributedCacheService>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        services.AddDbContext<EduZimDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>());
        });

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<EduZimDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.SigningKey) && s.SigningKey.Length >= 32,
                "Jwt:SigningKey must be configured and at least 32 characters.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwt) =>
            {
                var s = jwt.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s.SigningKey)),
                    ValidateIssuer = true,
                    ValidIssuer = s.Issuer,
                    ValidateAudience = true,
                    ValidAudience = s.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                };
            });

        services.AddSingleton<JwtAccessTokenIssuer>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddHangfire((_, config) =>
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<ICurrentUserInitializer>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<ITenantLifecycleChecker, TenantLifecycleChecker>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
