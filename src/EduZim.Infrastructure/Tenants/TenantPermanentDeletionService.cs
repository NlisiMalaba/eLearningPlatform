using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Infrastructure.Tenants;

public sealed class TenantPermanentDeletionService : ITenantPermanentDeletionService
{
    private readonly EduZimDbContext _db;
    private readonly ILogger<TenantPermanentDeletionService> _logger;

    public TenantPermanentDeletionService(EduZimDbContext db, ILogger<TenantPermanentDeletionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
        {
            _logger.LogWarning("Permanent deletion skipped: tenant {TenantId} not found.", tenantId);
            return;
        }

        if (tenant.Status != TenantStatus.Suspended)
        {
            _logger.LogInformation(
                "Permanent deletion skipped: tenant {TenantId} is not suspended (status {Status}).",
                tenantId,
                tenant.Status);
            return;
        }

        _logger.LogWarning("Starting permanent deletion for suspended tenant {TenantId}.", tenantId);

        await DeleteTenantDataAsync(tenantId, cancellationToken).ConfigureAwait(false);

        _logger.LogWarning("Completed permanent deletion for tenant {TenantId}.", tenantId);
    }

    private async Task DeleteTenantDataAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await _db.AnswerRecords.Where(a => a.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.AssessmentAttempts.Where(a => a.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.Assessments.Where(a => a.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.ModuleContentItems.Where(m => m.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.ContentItems.Where(c => c.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.Modules.Where(m => m.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.StudentProgresses.Where(s => s.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.StudentPoints.Where(s => s.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.Badges.Where(b => b.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.Notifications.Where(n => n.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.OfflineSyncQueues.Where(o => o.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.AuditLogs.Where(a => a.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.ParentStudentLinks.Where(p => p.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await _db.TenantInviteCodes.Where(i => i.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        var subscriptionIds = await _db.Subscriptions.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (subscriptionIds.Count > 0)
        {
            await _db.SubscriptionInvoices.Where(i => subscriptionIds.Contains(i.SubscriptionId))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            await _db.Payments.Where(p => subscriptionIds.Contains(p.SubscriptionId))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        await _db.Subscriptions.Where(s => s.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        var userIds = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (userIds.Count > 0)
        {
            await _db.RefreshTokens.Where(r => userIds.Contains(r.UserId)).ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            await _db.Users.Where(u => u.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        await _db.Tenants.Where(t => t.Id == tenantId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
