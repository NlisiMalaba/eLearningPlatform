namespace EduZim.Application.Common.Interfaces;

public interface ITenantLifecycleChecker
{
    Task<bool> IsTenantSuspendedAsync(Guid tenantId, CancellationToken cancellationToken);
}
