using EduZim.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EduZim.Tests.Integration.Support;

public sealed class EduZimWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly IReadOnlyDictionary<string, string?> _additionalSettings;

    public EduZimWebApplicationFactory(
        string connectionString,
        IReadOnlyDictionary<string, string?>? additionalSettings = null)
    {
        _connectionString = connectionString;
        _additionalSettings = additionalSettings ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            Dictionary<string, string?> settings = new()
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Redis:ConnectionString"] = string.Empty,
            };
            foreach (KeyValuePair<string, string?> pair in _additionalSettings)
                settings[pair.Key] = pair.Value;

            config.AddInMemoryCollection(settings);
        });
    }
}
