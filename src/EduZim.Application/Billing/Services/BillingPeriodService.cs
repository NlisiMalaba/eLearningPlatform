using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Billing.Services;

public sealed class BillingPeriodService : IBillingPeriodService
{
    private readonly BillingPricingOptions _options;

    public BillingPeriodService(IOptions<BillingPricingOptions> options)
    {
        _options = options.Value;
    }

    public DateTime AddBillingPeriod(DateTime anchorUtc, BillingCycle cycle)
    {
        return cycle switch
        {
            BillingCycle.Monthly => anchorUtc.AddMonths(1),
            BillingCycle.Yearly => anchorUtc.AddYears(1),
            BillingCycle.Termly => anchorUtc.AddMonths(_options.TermlyPeriodMonths),
            _ => anchorUtc,
        };
    }
}
