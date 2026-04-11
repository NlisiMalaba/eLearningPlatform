using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Billing.Commands.HandlePaymentFailed;

public sealed class HandlePaymentFailedCommandHandler : IRequestHandler<HandlePaymentFailedCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<HandlePaymentFailedCommandHandler> _logger;

    public HandlePaymentFailedCommandHandler(
        IEduZimDbContext db,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<HandlePaymentFailedCommandHandler> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Unit> Handle(HandlePaymentFailedCommand request, CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.TenantId == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
            throw new NotFoundException(nameof(Subscription), request.SubscriptionId);

        var graceEnd = DateTime.UtcNow.AddDays(7);
        subscription.Status = SubscriptionStatus.GracePeriod;
        subscription.GracePeriodEnd = graceEnd;

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _publisher.Publish(
                new PaymentFailedNotification(
                    request.TenantId,
                    request.SubscriptionId,
                    request.FailureReason,
                    request.PaymentProviderReference),
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogWarning(
            "Payment failed for subscription {SubscriptionId}; grace until {GraceEndUtc}.",
            subscription.Id,
            graceEnd);

        return Unit.Value;
    }
}
