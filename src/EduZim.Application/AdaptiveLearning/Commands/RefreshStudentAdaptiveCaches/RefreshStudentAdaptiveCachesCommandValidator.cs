using FluentValidation;

namespace EduZim.Application.AdaptiveLearning.Commands.RefreshStudentAdaptiveCaches;

public sealed class RefreshStudentAdaptiveCachesCommandValidator
    : AbstractValidator<RefreshStudentAdaptiveCachesCommand>
{
    public RefreshStudentAdaptiveCachesCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
    }
}
