using FluentValidation;

namespace EduZim.Application.Marketplace.Commands.RateContentPack;

public sealed class RateContentPackCommandValidator : AbstractValidator<RateContentPackCommand>
{
    public RateContentPackCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ContentPackId).NotEmpty();
        RuleFor(c => c.Rating).InclusiveBetween(1, 5);
        RuleFor(c => c.Review).MaximumLength(2000);
    }
}
