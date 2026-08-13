using Hangfire;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Hangfire entry: generate a printable progress certificate after a badge award.</summary>
public sealed class GenerateBadgeCertificateJob
{
    private readonly BadgeCertificateGenerationService _certificates;

    public GenerateBadgeCertificateJob(BadgeCertificateGenerationService certificates)
    {
        _certificates = certificates;
    }

    [AutomaticRetry(Attempts = 3)]
    public Task RunAsync(Guid tenantId, Guid studentId, Guid badgeId) =>
        _certificates.GenerateAndStoreAsync(tenantId, studentId, badgeId, CancellationToken.None);
}
