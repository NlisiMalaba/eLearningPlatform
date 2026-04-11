using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using EduZim.API.ExceptionHandling;
using EduZim.API.Middleware;
using EduZim.Application;
using EduZim.Application.Common.Configuration;
using EduZim.Infrastructure;
using EduZim.Infrastructure.Jobs;
using Hangfire;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = "EduZim API";
        document.Info.Version = "v1";
        document.Info.Description =
            "EduZim e-learning platform API (Zimbabwe). Use POST /api/v1/auth/login to obtain a JWT, then authorize requests with Bearer.";

        var components = document.Components ??= new OpenApiComponents();
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT access token from POST /api/v1/auth/login.",
        };

        return Task.CompletedTask;
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("EduZim API")
            .WithTheme(ScalarTheme.BluePlanet)
            .AddPreferredSecuritySchemes("Bearer");
    });
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<AuditMiddleware>();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapHangfireDashboard("/hangfire");
}

var renewalHour = Math.Clamp(
    app.Services.GetRequiredService<IOptions<BillingPricingOptions>>().Value.RenewalReminderUtcHour,
    0,
    23);
RecurringJob.AddOrUpdate<SubscriptionRenewalReminderJob>(
    "subscription-renewal-reminders",
    job => job.RunAsync(),
    Cron.Daily(renewalHour),
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

app.Run();
