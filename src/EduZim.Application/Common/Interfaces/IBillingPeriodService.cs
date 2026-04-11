using EduZim.Domain.Enums;

namespace EduZim.Application.Common.Interfaces;

/// <summary>Computes subscription period boundaries from a billing cycle.</summary>
public interface IBillingPeriodService
{
    /// <summary>Adds one full billing period starting at <paramref name="anchorUtc"/>.</summary>
    DateTime AddBillingPeriod(DateTime anchorUtc, BillingCycle cycle);
}
