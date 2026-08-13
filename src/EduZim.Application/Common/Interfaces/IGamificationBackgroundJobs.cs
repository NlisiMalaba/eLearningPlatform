namespace EduZim.Application.Common.Interfaces;

/// <summary>Queues printable progress-certificate generation after a badge is awarded.</summary>
public interface IGamificationBackgroundJobs
{
    string? EnqueueCertificateGeneration(Guid tenantId, Guid studentId, Guid badgeId);
}
