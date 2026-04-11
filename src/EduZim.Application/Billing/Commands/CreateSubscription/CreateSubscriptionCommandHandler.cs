using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Billing.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Guid>
{
    private readonly IEduZimDbContext _db;
    private readonly IBillingPricingService _pricing;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

    public CreateSubscriptionCommandHandler(
        IEduZimDbContext db,
        IBillingPricingService pricing,
        ILogger<CreateSubscriptionCommandHandler> logger)
    {
        _db = db;
        _pricing = pricing;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        _pricing.EnsureCycleAllowedForTier(tenant.Tier, request.Cycle);

        var duplicate = await _db.Subscriptions.AsNoTracking()
            .AnyAsync(s => s.TenantId == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate)
            throw new ConflictException("A subscription already exists for this tenant.");

        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();
        var subscription = new Subscription
        {
            Id = id,
            TenantId = request.TenantId,
            Cycle = request.Cycle,
            Status = SubscriptionStatus.Suspended,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now,
            GracePeriodEnd = null,
            StudentCount = request.StudentCount,
        };

        await _db.Subscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Created subscription {SubscriptionId} for tenant {TenantId}, cycle {Cycle}, students {StudentCount}.",
            id,
            request.TenantId,
            request.Cycle,
            request.StudentCount);

        return id;
    }
}
