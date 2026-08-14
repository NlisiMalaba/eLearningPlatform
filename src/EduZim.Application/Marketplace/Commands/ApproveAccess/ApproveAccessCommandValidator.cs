using FluentValidation;

namespace EduZim.Application.Marketplace.Commands.ApproveAccess;

public sealed class ApproveAccessCommandValidator : AbstractValidator<ApproveAccessCommand>
{
    public ApproveAccessCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ContentPackId).NotEmpty();
        RuleFor(c => c.RequestingTenantId).NotEmpty();
        RuleFor(c => c.RequestingTenantId)
            .NotEqual(c => c.TenantId)
            .WithMessage("The requesting tenant must be different from the originating tenant.");
    }
}
