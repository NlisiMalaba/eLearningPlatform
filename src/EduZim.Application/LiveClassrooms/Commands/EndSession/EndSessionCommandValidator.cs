using FluentValidation;

namespace EduZim.Application.LiveClassrooms.Commands.EndSession;

public sealed class EndSessionCommandValidator : AbstractValidator<EndSessionCommand>
{
    public EndSessionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SessionId).NotEmpty();
    }
}
