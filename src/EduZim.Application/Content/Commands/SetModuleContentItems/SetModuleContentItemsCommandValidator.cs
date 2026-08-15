using FluentValidation;

namespace EduZim.Application.Content.Commands.SetModuleContentItems;

public sealed class SetModuleContentItemsCommandValidator : AbstractValidator<SetModuleContentItemsCommand>
{
    public SetModuleContentItemsCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ModuleId).NotEmpty();
        RuleFor(c => c.ContentItemIds).NotNull();
        RuleFor(c => c.ContentItemIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Content item ids must be unique.");
        RuleForEach(c => c.ContentItemIds).NotEmpty();
    }
}
