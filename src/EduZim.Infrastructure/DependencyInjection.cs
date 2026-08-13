using System.Text;
using EduZim.Application.Billing.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Configuration;
using EduZim.Infrastructure.Billing;
using EduZim.Infrastructure.Jobs;
using EduZim.Domain.Entities;
using EduZim.Infrastructure.Email;
using EduZim.Infrastructure.Sms;
using EduZim.Infrastructure.Audit;
using EduZim.Infrastructure.Caching;
using EduZim.Infrastructure.Identity;
using EduZim.Infrastructure.Persistence;
using EduZim.Infrastructure.Persistence.Interceptors;
using Amazon;
using Amazon.S3;
using EduZim.Infrastructure.Content;
using EduZim.Infrastructure.Storage;
using EduZim.Infrastructure.Assessments;
using EduZim.Infrastructure.Gamification;
using EduZim.Infrastructure.Ai;
using EduZim.Infrastructure.Notifications;
using EduZim.Infrastructure.Tenants;
using EduZim.Infrastructure.Video;
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

        services.Configure<AzureOpenAiOptions>(configuration.GetSection(AzureOpenAiOptions.SectionName));
        services.Configure<IdentityAppSettings>(configuration.GetSection(IdentityAppSettings.SectionName));
        services.Configure<TenantLifecycleSettings>(configuration.GetSection(TenantLifecycleSettings.SectionName));
        services.Configure<BillingPricingOptions>(configuration.GetSection(BillingPricingOptions.SectionName));
        services.Configure<ContentStorageOptions>(configuration.GetSection(ContentStorageOptions.SectionName));

        services.AddSingleton<IStorageService>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ContentStorageOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opts.BucketName))
            {
                var regionName = string.IsNullOrWhiteSpace(opts.Region) ? "us-east-1" : opts.Region;
                var client = new AmazonS3Client(RegionEndpoint.GetBySystemName(regionName));
                return new S3StorageService(client, sp.GetRequiredService<IOptions<ContentStorageOptions>>());
            }

            return new LocalFileStorageService(sp.GetRequiredService<IOptions<ContentStorageOptions>>());
        });

        services.AddScoped<IBillingPricingService, BillingPricingService>();
        services.AddScoped<IBillingPeriodService, BillingPeriodService>();
        services.AddScoped<IBillingInvoiceService, BillingInvoiceService>();
        services.AddSingleton<IInvoicePdfGenerator, QuestPdfInvoiceGenerator>();
        services.AddScoped<ISubscriptionRenewalReminderService, SubscriptionRenewalReminderService>();
        services.AddScoped<IBillingStripeWebhookProcessor, BillingStripeWebhookProcessor>();

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<EduZimDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddSingleton<IEmailService, NullEmailService>();
        services.AddSingleton<ISmsService, NullSmsService>();
        services.AddSingleton<IVideoService, NullVideoService>();
        services.AddSingleton<AzureOpenAiKernelAccessor>();
        services.AddSingleton(AiResiliencePipeline.Create());
        services.AddScoped<IAiService, SemanticKernelAiService>();

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

        services.AddScoped<JwtAccessTokenIssuer>();
        services.AddScoped<IAccessTokenIssuer>(sp => sp.GetRequiredService<JwtAccessTokenIssuer>());
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

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
        services.AddScoped<IEduZimDbContext>(sp => sp.GetRequiredService<EduZimDbContext>());
        services.AddScoped<ITenantBackgroundJobs, TenantBackgroundJobs>();
        services.AddScoped<ITenantPermanentDeletionService, TenantPermanentDeletionService>();
        services.AddScoped<TenantPermanentDeletionJob>();
        services.AddScoped<IContentBackgroundJobs, ContentBackgroundJobs>();
        services.AddScoped<IContentPermanentDeletionService, ContentPermanentDeletionService>();
        services.AddScoped<ContentPermanentDeletionJob>();
        services.AddScoped<IAssessmentBackgroundJobs, AssessmentBackgroundJobs>();
        services.AddScoped<AssessmentTimedAutoSubmitJob>();
        services.AddScoped<IGamificationBackgroundJobs, GamificationBackgroundJobs>();
        services.AddScoped<INotificationBackgroundJobs, NotificationBackgroundJobs>();
        services.AddScoped<RetryFailedSmsJob>();
        services.AddScoped<BadgeCertificateGenerationService>();
        services.AddScoped<GenerateBadgeCertificateJob>();
        services.AddScoped<SubscriptionRenewalReminderJob>();
        services.AddScoped<AdaptiveLearningWeeklySummaryJob>();
        services.AddScoped<StudentInactivityAlertJob>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
