using FluentValidation;

namespace EduZim.Application.Content.Queries.ListContent;

public sealed class ListContentQueryValidator : AbstractValidator<ListContentQuery>
{
    public ListContentQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
