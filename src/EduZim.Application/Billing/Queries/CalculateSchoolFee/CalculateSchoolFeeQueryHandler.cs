using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Billing.Queries.CalculateSchoolFee;

public sealed class CalculateSchoolFeeQueryHandler : IRequestHandler<CalculateSchoolFeeQuery, decimal>
{
    private readonly IEduZimDbContext _db;
    private readonly IBillingPricingService _pricing;

    public CalculateSchoolFeeQueryHandler(IEduZimDbContext db, IBillingPricingService pricing)
    {
        _db = db;
        _pricing = pricing;
    }

    public async Task<decimal> Handle(CalculateSchoolFeeQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        var unitPrice = _pricing.GetUnitPrice(tenant.Tier, request.Cycle);
        return unitPrice * request.StudentCount;
    }
}
