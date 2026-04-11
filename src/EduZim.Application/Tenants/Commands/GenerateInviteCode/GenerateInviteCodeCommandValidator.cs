using FluentValidation;

namespace EduZim.Application.Tenants.Commands.GenerateInviteCode;

public sealed class GenerateInviteCodeCommandValidator : AbstractValidator<GenerateInviteCodeCommand>
{
    public GenerateInviteCodeCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentUserId).NotEmpty();
    }
}
