using FluentValidation;

namespace EduZim.Application.Assessments.Commands.AssignAssessment;

public sealed class AssignAssessmentCommandValidator : AbstractValidator<AssignAssessmentCommand>
{
    public AssignAssessmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AssessmentId).NotEmpty();
        RuleFor(c => c.SchoolClassId).NotEmpty();
    }
}
