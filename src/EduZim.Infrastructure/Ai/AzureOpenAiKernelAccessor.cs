using EduZim.Application.Common.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace EduZim.Infrastructure.Ai;

internal sealed class AzureOpenAiKernelAccessor
{
    public AzureOpenAiKernelAccessor(IOptions<AzureOpenAiOptions> options)
    {
        Kernel = TryCreate(options.Value);
    }

    public Kernel? Kernel { get; }

    private static Kernel? TryCreate(AzureOpenAiOptions options)
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
        builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);
        return builder.Build();
    }
}
