using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Enums;
using FluentValidation.Results;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Billing.Services;

public sealed class BillingPricingService : IBillingPricingService
{
    private readonly BillingPricingOptions _options;

    public BillingPricingService(IOptions<BillingPricingOptions> options)
    {
        _options = options.Value;
    }

    public void EnsureCycleAllowedForTier(TenantTier tier, BillingCycle cycle)
    {
        _ = GetUnitPrice(tier, cycle);
    }

    public decimal GetUnitPrice(TenantTier tier, BillingCycle cycle)
    {
        return tier switch
        {
            TenantTier.PreSchool => cycle switch
            {
                BillingCycle.Monthly => _options.PreSchoolMonthly,
                BillingCycle.Yearly => _options.PreSchoolYearly,
                BillingCycle.Termly => throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(cycle), "Termly billing is not available for the Pre-school tier."),
                }),
                _ => throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(cycle), "Unknown billing cycle."),
                }),
            },
            TenantTier.School => cycle switch
            {
                BillingCycle.Monthly => _options.SchoolMonthly,
                BillingCycle.Termly => _options.SchoolTermly,
                BillingCycle.Yearly => _options.SchoolYearly,
                _ => throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(cycle), "Unknown billing cycle."),
                }),
            },
            _ => throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(tier), "Unknown tenant tier."),
            }),
        };
    }
}
