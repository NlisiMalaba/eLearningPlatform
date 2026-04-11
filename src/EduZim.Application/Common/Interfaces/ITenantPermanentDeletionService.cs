namespace EduZim.Application.Common.Interfaces;

/// <summary>Permanently removes tenant-scoped data after the suspension retention period.</summary>
public interface ITenantPermanentDeletionService
{
    Task ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
