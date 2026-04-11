using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Billing.Commands.HandlePaymentSucceeded;

public sealed class HandlePaymentSucceededCommandHandler : IRequestHandler<HandlePaymentSucceededCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBillingPeriodService _periodService;
    private readonly IBillingInvoiceService _invoiceService;
    private readonly ILogger<HandlePaymentSucceededCommandHandler> _logger;

    public HandlePaymentSucceededCommandHandler(
        IEduZimDbContext db,
        IUnitOfWork unitOfWork,
        IBillingPeriodService periodService,
        IBillingInvoiceService invoiceService,
        ILogger<HandlePaymentSucceededCommandHandler> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _periodService = periodService;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task<Unit> Handle(HandlePaymentSucceededCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var alreadyProcessed = await _db.Payments.AsNoTracking()
                .AnyAsync(p => p.IdempotencyKey == request.IdempotencyKey, cancellationToken)
                .ConfigureAwait(false);
            if (alreadyProcessed)
            {
                _logger.LogInformation(
                    "Skipping duplicate payment success for idempotency key {IdempotencyKey}.",
                    request.IdempotencyKey);
                return Unit.Value;
            }
        }

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.TenantId == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
            throw new NotFoundException(nameof(Subscription), request.SubscriptionId);

        var now = DateTime.UtcNow;
        if (subscription.CurrentPeriodEnd > now && subscription.Status == SubscriptionStatus.Active)
        {
            subscription.CurrentPeriodEnd = _periodService.AddBillingPeriod(subscription.CurrentPeriodEnd, subscription.Cycle);
        }
        else
        {
            subscription.CurrentPeriodStart = now;
            subscription.CurrentPeriodEnd = _periodService.AddBillingPeriod(now, subscription.Cycle);
        }

        subscription.Status = SubscriptionStatus.Active;
        subscription.GracePeriodEnd = null;
        subscription.RenewalReminderSentForPeriodEndUtc = null;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            Amount = request.Amount,
            Currency = request.Currency,
            ProviderReference = request.PaymentProviderReference,
            IdempotencyKey = request.IdempotencyKey,
            CreatedAtUtc = now,
        };

        await _db.Payments.AddAsync(payment, cancellationToken).ConfigureAwait(false);
        await _invoiceService.CreateOrGetInvoiceAsync(payment, subscription, tenant, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Payment succeeded for subscription {SubscriptionId}, payment {PaymentId}.",
            subscription.Id,
            payment.Id);

        return Unit.Value;
    }
}
