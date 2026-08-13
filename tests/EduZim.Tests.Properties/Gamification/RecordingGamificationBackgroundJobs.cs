using EduZim.Application.Common.Interfaces;

namespace EduZim.Tests.Properties.Gamification;

internal sealed class RecordingGamificationBackgroundJobs : IGamificationBackgroundJobs
{
    public List<(Guid TenantId, Guid StudentId, Guid BadgeId)> Queued { get; } = new();

    public string? EnqueueCertificateGeneration(Guid tenantId, Guid studentId, Guid badgeId)
    {
        Queued.Add((tenantId, studentId, badgeId));
        return "cert-job";
    }
}
