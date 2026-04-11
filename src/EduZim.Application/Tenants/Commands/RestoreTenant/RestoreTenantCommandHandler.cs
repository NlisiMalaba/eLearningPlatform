using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Tenants.Commands.RestoreTenant;

public sealed class RestoreTenantCommandHandler : IRequestHandler<RestoreTenantCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantBackgroundJobs _backgroundJobs;
    private readonly ILogger<RestoreTenantCommandHandler> _logger;

    public RestoreTenantCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ITenantBackgroundJobs backgroundJobs,
        ILogger<RestoreTenantCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task<Unit> Handle(RestoreTenantCommand request, CancellationToken cancellationToken)
    {
        TenantAccessHelper.EnsurePlatformAdmin(_currentUser);

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        if (tenant.Status != TenantStatus.Suspended)
            return Unit.Value;

        _backgroundJobs.TryCancelJob(tenant.PermanentDeletionHangfireJobId);

        tenant.Status = TenantStatus.Active;
        tenant.SuspendedAtUtc = null;
        tenant.PermanentDeletionHangfireJobId = null;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Tenant {TenantId} restored to active status.", tenant.Id);
        return Unit.Value;
    }
}
