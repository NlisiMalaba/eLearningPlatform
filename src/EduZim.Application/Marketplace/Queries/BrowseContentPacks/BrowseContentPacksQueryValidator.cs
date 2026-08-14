using FluentValidation;

namespace EduZim.Application.Marketplace.Queries.BrowseContentPacks;

public sealed class BrowseContentPacksQueryValidator : AbstractValidator<BrowseContentPacksQuery>
{
    public BrowseContentPacksQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
