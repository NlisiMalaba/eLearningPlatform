using FluentValidation;

namespace EduZim.Application.Tenants.Queries.GetTenant;

public sealed class GetTenantQueryValidator : AbstractValidator<GetTenantQuery>
{
    public GetTenantQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
