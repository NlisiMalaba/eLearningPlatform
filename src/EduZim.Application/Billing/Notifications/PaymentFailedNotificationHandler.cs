using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Billing.Notifications;

/// <summary>Keeps tenant access during grace period and notifies subscribers after a failed payment.</summary>
public sealed class PaymentFailedNotificationHandler : INotificationHandler<PaymentFailedNotification>
{
    private readonly IEduZimDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentFailedNotificationHandler> _logger;

    public PaymentFailedNotificationHandler(
        IEduZimDbContext db,
        IMediator mediator,
        ILogger<PaymentFailedNotificationHandler> logger)
    {
        _db = db;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(PaymentFailedNotification notification, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(notification.TenantId, ct).ConfigureAwait(false);

        Tenant? tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == notification.TenantId, ct)
            .ConfigureAwait(false);
        if (tenant is null)
        {
            _logger.LogWarning(
                "Payment failed for missing tenant {TenantId}; skipping subscriber notification.",
                notification.TenantId);
            return;
        }

        if (tenant.Status == TenantStatus.Provisioning)
        {
            tenant.Status = TenantStatus.Active;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        string message = BillingPaymentMessages.PaymentFailed(tenant.Name, notification.FailureReason);
        int notified = await BillingSubscriberFanout
            .NotifyAsync(_db, _mediator, notification.TenantId, NotificationType.PaymentFailed, message, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Payment failed for tenant {TenantId}: status {Status}, notified {Count} subscriber(s) within {SlaSeconds}s.",
            notification.TenantId,
            tenant.Status,
            notified,
            BillingPaymentMessages.SubscriberNotifySlaSeconds);
    }
}
