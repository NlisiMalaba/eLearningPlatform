using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Tenants.Commands.SuspendTenant;

public sealed class SuspendTenantCommandHandler : IRequestHandler<SuspendTenantCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantBackgroundJobs _backgroundJobs;
    private readonly TenantLifecycleSettings _lifecycle;
    private readonly ILogger<SuspendTenantCommandHandler> _logger;

    public SuspendTenantCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ITenantBackgroundJobs backgroundJobs,
        IOptions<TenantLifecycleSettings> lifecycle,
        ILogger<SuspendTenantCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _backgroundJobs = backgroundJobs;
        _lifecycle = lifecycle.Value;
        _logger = logger;
    }

    public async Task<Unit> Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        TenantAccessHelper.EnsurePlatformAdmin(_currentUser);

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        if (tenant.Status == TenantStatus.Suspended)
            return Unit.Value;

        tenant.Status = TenantStatus.Suspended;
        tenant.SuspendedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(tenant.PermanentDeletionHangfireJobId))
            _backgroundJobs.TryCancelJob(tenant.PermanentDeletionHangfireJobId);

        var runAt = tenant.SuspendedAtUtc.Value.AddDays(_lifecycle.SuspendedTenantRetentionDays);
        tenant.PermanentDeletionHangfireJobId = _backgroundJobs.SchedulePermanentDeletionAt(tenant.Id, runAt);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Tenant {TenantId} suspended; permanent deletion scheduled at {RunAtUtc} (job {JobId}).",
            tenant.Id,
            runAt,
            tenant.PermanentDeletionHangfireJobId);
        return Unit.Value;
    }
}
