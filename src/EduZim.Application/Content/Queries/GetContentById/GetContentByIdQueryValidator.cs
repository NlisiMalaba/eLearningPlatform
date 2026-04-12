using FluentValidation;

namespace EduZim.Application.Content.Queries.GetContentById;

public sealed class GetContentByIdQueryValidator : AbstractValidator<GetContentByIdQuery>
{
    public GetContentByIdQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.ContentId).NotEmpty();
    }
}
