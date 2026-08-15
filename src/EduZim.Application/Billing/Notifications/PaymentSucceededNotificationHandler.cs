using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Billing.Notifications;

/// <summary>Activates tenant access and notifies subscribers after a successful payment.</summary>
public sealed class PaymentSucceededNotificationHandler : INotificationHandler<PaymentSucceededNotification>
{
    private readonly IEduZimDbContext _db;
    private readonly ITenantBackgroundJobs _tenantJobs;
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentSucceededNotificationHandler> _logger;

    public PaymentSucceededNotificationHandler(
        IEduZimDbContext db,
        ITenantBackgroundJobs tenantJobs,
        IMediator mediator,
        ILogger<PaymentSucceededNotificationHandler> logger)
    {
        _db = db;
        _tenantJobs = tenantJobs;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(PaymentSucceededNotification notification, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(notification.TenantId, ct).ConfigureAwait(false);

        Tenant? tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == notification.TenantId, ct)
            .ConfigureAwait(false);
        if (tenant is null)
        {
            _logger.LogWarning(
                "Payment succeeded for missing tenant {TenantId}; skipping status update.",
                notification.TenantId);
            return;
        }

        ActivateTenantIfNeeded(tenant);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        string message = BillingPaymentMessages.PaymentSucceeded(tenant.Name);
        int notified = await BillingSubscriberFanout
            .NotifyAsync(_db, _mediator, notification.TenantId, NotificationType.PaymentSucceeded, message, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Payment succeeded for tenant {TenantId}: status {Status}, notified {Count} subscriber(s) within {SlaSeconds}s.",
            notification.TenantId,
            tenant.Status,
            notified,
            BillingPaymentMessages.SubscriberNotifySlaSeconds);
    }

    private void ActivateTenantIfNeeded(Tenant tenant)
    {
        if (tenant.Status == TenantStatus.Active && tenant.SuspendedAtUtc is null)
            return;

        _tenantJobs.TryCancelJob(tenant.PermanentDeletionHangfireJobId);
        tenant.Status = TenantStatus.Active;
        tenant.SuspendedAtUtc = null;
        tenant.PermanentDeletionHangfireJobId = null;
    }
}
