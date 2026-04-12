using FluentValidation;

namespace EduZim.Application.Content.Queries.GetModuleById;

public sealed class GetModuleByIdQueryValidator : AbstractValidator<GetModuleByIdQuery>
{
    public GetModuleByIdQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.ModuleId).NotEmpty();
    }
}
