namespace EduZim.Application.Common.Configuration;

public sealed class BillingPricingOptions
{
    public const string SectionName = "Billing";

    /// <summary>Length of one school term when using <see cref="BillingCycle.Termly"/>.</summary>
    public int TermlyPeriodMonths { get; set; } = 4;

    public decimal PreSchoolMonthly { get; set; } = 5m;
    public decimal PreSchoolYearly { get; set; } = 50m;
    public decimal SchoolMonthly { get; set; } = 8m;
    public decimal SchoolTermly { get; set; } = 22m;
    public decimal SchoolYearly { get; set; } = 75m;
}
