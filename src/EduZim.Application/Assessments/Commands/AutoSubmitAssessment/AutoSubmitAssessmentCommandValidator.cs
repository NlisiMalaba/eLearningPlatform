using FluentValidation;

namespace EduZim.Application.Assessments.Commands.AutoSubmitAssessment;

public sealed class AutoSubmitAssessmentCommandValidator : AbstractValidator<AutoSubmitAssessmentCommand>
{
    public AutoSubmitAssessmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttemptId).NotEmpty();
    }
}
