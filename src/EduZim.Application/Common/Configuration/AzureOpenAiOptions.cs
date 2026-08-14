namespace EduZim.Application.Common.Configuration;

public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string? DeploymentName { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(DeploymentName);
}
