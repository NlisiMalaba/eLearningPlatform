using FluentValidation;

namespace EduZim.Application.Assessments.Commands.BeginAssessmentSession;

public sealed class BeginAssessmentSessionCommandValidator : AbstractValidator<BeginAssessmentSessionCommand>
{
    public BeginAssessmentSessionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AssessmentId).NotEmpty();
        RuleFor(c => c.SchoolClassId).NotEmpty();
    }
}
