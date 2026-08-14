using FluentValidation;

namespace EduZim.Application.Progress.Commands.RecordStudentInteraction;

public sealed class RecordStudentInteractionCommandValidator
    : AbstractValidator<RecordStudentInteractionCommand>
{
    public RecordStudentInteractionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
    }
}
