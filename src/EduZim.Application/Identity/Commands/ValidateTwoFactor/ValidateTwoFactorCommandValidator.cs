using FluentValidation;

namespace EduZim.Application.Identity.Commands.ValidateTwoFactor;

public sealed class ValidateTwoFactorCommandValidator : AbstractValidator<ValidateTwoFactorCommand>
{
    public ValidateTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(6, 10);
    }
}
