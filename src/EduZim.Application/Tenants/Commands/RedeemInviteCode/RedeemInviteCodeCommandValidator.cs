using FluentValidation;

namespace EduZim.Application.Tenants.Commands.RedeemInviteCode;

public sealed class RedeemInviteCodeCommandValidator : AbstractValidator<RedeemInviteCodeCommand>
{
    public RedeemInviteCodeCommandValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
    }
}
