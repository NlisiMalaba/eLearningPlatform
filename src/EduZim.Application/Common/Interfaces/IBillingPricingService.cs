using EduZim.Domain.Enums;

namespace EduZim.Application.Common.Interfaces;

public interface IBillingPricingService
{
    /// <summary>Returns the unit price for one student for the tier and cycle.</summary>
    /// <exception cref="Exceptions.ValidationException">When the cycle is not sold for the tier.</exception>
    decimal GetUnitPrice(TenantTier tier, BillingCycle cycle);

    /// <summary>Validates requirement 2.1 / 2.2 (tier-specific cycles).</summary>
    void EnsureCycleAllowedForTier(TenantTier tier, BillingCycle cycle);
}
