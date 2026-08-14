using FluentValidation;

namespace EduZim.Application.Marketplace.Commands.ApproveContentPack;

public sealed class ApproveContentPackCommandValidator : AbstractValidator<ApproveContentPackCommand>
{
    public ApproveContentPackCommandValidator()
    {
        RuleFor(c => c.ContentPackId).NotEmpty();
    }
}
