using System.Security.Authentication;
using EduZim.API.Middleware;

namespace EduZim.API;

internal static class TransportSecurityExtensions
{
    public static readonly TimeSpan HstsMaxAge = TimeSpan.FromDays(365);

    public static WebApplicationBuilder AddTransportSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.AddHsts(options =>
        {
            options.MaxAge = HstsMaxAge;
            options.IncludeSubDomains = true;
            options.Preload = true;
        });

        builder.WebHost.ConfigureKestrel(server =>
        {
            server.ConfigureHttpsDefaults(https =>
            {
                https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            });
        });

        return builder;
    }

    public static WebApplication UseTransportSecurity(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        app.UseHttpsRedirection();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
