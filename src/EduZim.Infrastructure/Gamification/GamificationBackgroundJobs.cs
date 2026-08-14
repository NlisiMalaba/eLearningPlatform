using EduZim.Application.Common.Interfaces;
using EduZim.Infrastructure.Jobs;
using Hangfire;

namespace EduZim.Infrastructure.Gamification;

public sealed class GamificationBackgroundJobs : IGamificationBackgroundJobs
{
    public string? EnqueueCertificateGeneration(Guid tenantId, Guid studentId, Guid badgeId)
    {
        return BackgroundJob.Enqueue<GenerateBadgeCertificateJob>(
            job => job.RunAsync(tenantId, studentId, badgeId));
    }
}
