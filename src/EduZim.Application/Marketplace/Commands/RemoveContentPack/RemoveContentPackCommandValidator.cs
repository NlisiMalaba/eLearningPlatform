using FluentValidation;

namespace EduZim.Application.Marketplace.Commands.RemoveContentPack;

public sealed class RemoveContentPackCommandValidator : AbstractValidator<RemoveContentPackCommand>
{
    public RemoveContentPackCommandValidator()
    {
        RuleFor(c => c.ContentPackId).NotEmpty();
    }
}
