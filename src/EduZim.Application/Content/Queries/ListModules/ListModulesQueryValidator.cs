using FluentValidation;

namespace EduZim.Application.Content.Queries.ListModules;

public sealed class ListModulesQueryValidator : AbstractValidator<ListModulesQuery>
{
    public ListModulesQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
