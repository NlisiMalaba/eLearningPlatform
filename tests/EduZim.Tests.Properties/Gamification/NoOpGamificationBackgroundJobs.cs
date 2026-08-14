using EduZim.Application.Common.Interfaces;

namespace EduZim.Tests.Properties.Gamification;

internal sealed class NoOpGamificationBackgroundJobs : IGamificationBackgroundJobs
{
    public string? EnqueueCertificateGeneration(Guid tenantId, Guid studentId, Guid badgeId) => "test-cert-job";
}
