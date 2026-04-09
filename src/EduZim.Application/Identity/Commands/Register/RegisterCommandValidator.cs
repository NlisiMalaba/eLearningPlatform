using FluentValidation;

namespace EduZim.Application.Identity.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(256);
        RuleFor(x => x.Role).IsInEnum();
    }
}
