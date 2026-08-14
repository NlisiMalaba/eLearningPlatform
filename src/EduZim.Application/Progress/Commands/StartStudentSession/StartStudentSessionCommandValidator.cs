using FluentValidation;

namespace EduZim.Application.Progress.Commands.StartStudentSession;

public sealed class StartStudentSessionCommandValidator : AbstractValidator<StartStudentSessionCommand>
{
    public StartStudentSessionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
    }
}
