using FluentValidation;

namespace EduZim.Application.Billing.Queries.CalculateSchoolFee;

public sealed class CalculateSchoolFeeQueryValidator : AbstractValidator<CalculateSchoolFeeQuery>
{
    public CalculateSchoolFeeQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.StudentCount).GreaterThan(0);
    }
}
