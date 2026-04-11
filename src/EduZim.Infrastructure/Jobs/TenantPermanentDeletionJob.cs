using EduZim.Application.Common.Interfaces;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Hangfire entry point for scheduled tenant data deletion.</summary>
public sealed class TenantPermanentDeletionJob
{
    private readonly ITenantPermanentDeletionService _deletion;

    public TenantPermanentDeletionJob(ITenantPermanentDeletionService deletion)
    {
        _deletion = deletion;
    }

    public Task RunAsync(Guid tenantId) => _deletion.ExecuteAsync(tenantId, CancellationToken.None);
}
