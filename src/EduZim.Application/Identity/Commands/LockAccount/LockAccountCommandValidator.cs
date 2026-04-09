using FluentValidation;

namespace EduZim.Application.Identity.Commands.LockAccount;

public sealed class LockAccountCommandValidator : AbstractValidator<LockAccountCommand>
{
    public LockAccountCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
