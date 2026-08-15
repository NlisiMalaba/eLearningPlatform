using EduZim.Application.Common.Configuration;
using EduZim.Infrastructure.Http;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace EduZim.Infrastructure.Ai;

internal sealed class AzureOpenAiKernelAccessor
{
    public AzureOpenAiKernelAccessor(IOptions<AzureOpenAiOptions> options, IHttpClientFactory httpClients)
    {
        Kernel = TryCreate(options.Value, httpClients.CreateClient(ExternalHttpClientNames.Ai));
    }

    public Kernel? Kernel { get; }

    private static Kernel? TryCreate(AzureOpenAiOptions options, HttpClient httpClient)
    {
        string? endpoint = options.Endpoint;
        string? apiKey = options.ApiKey;
        string? deployment = options.DeploymentName;
        if (string.IsNullOrWhiteSpace(endpoint)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(deployment))
        {
            return null;
        }

        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: deployment,
            endpoint: endpoint,
            apiKey: apiKey,
            httpClient: httpClient);
        return builder.Build();
    }
}
