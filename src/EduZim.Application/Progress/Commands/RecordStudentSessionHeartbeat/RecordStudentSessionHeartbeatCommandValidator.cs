using FluentValidation;

namespace EduZim.Application.Progress.Commands.RecordStudentSessionHeartbeat;

public sealed class RecordStudentSessionHeartbeatCommandValidator
    : AbstractValidator<RecordStudentSessionHeartbeatCommand>
{
    public RecordStudentSessionHeartbeatCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
    }
}
