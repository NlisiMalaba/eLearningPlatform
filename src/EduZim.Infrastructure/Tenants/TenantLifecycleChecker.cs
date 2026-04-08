using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Infrastructure.Tenants;

public sealed class TenantLifecycleChecker : ITenantLifecycleChecker
{
    private readonly EduZimDbContext _db;

    public TenantLifecycleChecker(EduZimDbContext db)
    {
        _db = db;
    }

    public Task<bool> IsTenantSuspendedAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.Id == tenantId && t.Status == TenantStatus.Suspended, cancellationToken);
    }
}
