using FluentValidation;

namespace EduZim.Application.Progress.Commands.ResumeStudentSession;

public sealed class ResumeStudentSessionCommandValidator
    : AbstractValidator<ResumeStudentSessionCommand>
{
    public ResumeStudentSessionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
    }
}
