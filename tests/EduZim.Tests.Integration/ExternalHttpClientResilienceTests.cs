using EduZim.Infrastructure.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Integration;

public sealed class ExternalHttpClientResilienceTests
{
    [Fact]
    public void Registers_named_http_clients_for_ai_sms_payment_and_video()
    {
        ServiceCollection services = new();
        services.AddLogging();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ExternalHttpClientExtensions.SmsBaseUrlKey] = "https://sms.example.test/",
                [ExternalHttpClientExtensions.PaymentBaseUrlKey] = "https://pay.example.test/",
                [ExternalHttpClientExtensions.VideoBaseUrlKey] = "https://video.example.test/",
            })
            .Build();

        services.AddResilientExternalHttpClients(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        Assert.Equal(
            "https://sms.example.test/",
            factory.CreateClient(ExternalHttpClientNames.Sms).BaseAddress?.ToString());
        Assert.Equal(
            "https://pay.example.test/",
            factory.CreateClient(ExternalHttpClientNames.Payment).BaseAddress?.ToString());
        Assert.Equal(
            "https://video.example.test/",
            factory.CreateClient(ExternalHttpClientNames.Video).BaseAddress?.ToString());
        Assert.NotNull(factory.CreateClient(ExternalHttpClientNames.Ai));
    }
}
