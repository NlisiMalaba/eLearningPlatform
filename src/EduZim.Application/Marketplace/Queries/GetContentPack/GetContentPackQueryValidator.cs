using FluentValidation;

namespace EduZim.Application.Marketplace.Queries.GetContentPack;

public sealed class GetContentPackQueryValidator : AbstractValidator<GetContentPackQuery>
{
    public GetContentPackQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.ContentPackId).NotEmpty();
    }
}
