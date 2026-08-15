using EduZim.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EduZim.Tests.Integration.Support;

/// <summary>
/// Host for auth-only HTTP tests: in-memory Hangfire and no PostgreSQL at startup.
/// </summary>
public sealed class UnauthorizedApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=127.0.0.1;Port=1;Database=eduzim_unused;Username=test;Password=test;Timeout=1;Command Timeout=1",
                ["Redis:ConnectionString"] = string.Empty,
                ["Hangfire:UseInMemory"] = "true",
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        catch (TaskCanceledException)
        {
            // Hosted services may cancel during teardown when PostgreSQL is not used.
        }
        catch (OperationCanceledException)
        {
        }
    }
}
