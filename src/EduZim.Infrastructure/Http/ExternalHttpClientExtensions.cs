using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace EduZim.Infrastructure.Http;

public static class ExternalHttpClientExtensions
{
    public const string SmsBaseUrlKey = "ExternalServices:Sms:BaseUrl";
    public const string PaymentBaseUrlKey = "ExternalServices:Payment:BaseUrl";
    public const string VideoBaseUrlKey = "ExternalServices:Video:BaseUrl";

    public static IServiceCollection AddResilientExternalHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddResilientClient(services, ExternalHttpClientNames.Ai, baseUrl: null);
        AddResilientClient(services, ExternalHttpClientNames.Sms, configuration[SmsBaseUrlKey]);
        AddResilientClient(services, ExternalHttpClientNames.Payment, configuration[PaymentBaseUrlKey]);
        AddResilientClient(services, ExternalHttpClientNames.Video, configuration[VideoBaseUrlKey]);
        return services;
    }

    private static void AddResilientClient(IServiceCollection services, string name, string? baseUrl)
    {
        services.AddHttpClient(name, client =>
            {
                if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri))
                    client.BaseAddress = uri;
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromMilliseconds(200);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.FailureRatio = 1.0;
            });
    }
}
