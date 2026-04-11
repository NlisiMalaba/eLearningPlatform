using FluentValidation;

namespace EduZim.Application.Tenants.Queries.GetTenantDashboard;

public sealed class GetTenantDashboardQueryValidator : AbstractValidator<GetTenantDashboardQuery>
{
    public GetTenantDashboardQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
