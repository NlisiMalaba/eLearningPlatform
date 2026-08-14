using FluentValidation;

namespace EduZim.Application.Marketplace.Commands.SubmitContentPack;

public sealed class SubmitContentPackCommandValidator : AbstractValidator<SubmitContentPackCommand>
{
    public SubmitContentPackCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.ContentItemIds).NotEmpty();
        RuleForEach(c => c.ContentItemIds).NotEmpty();
    }
}
