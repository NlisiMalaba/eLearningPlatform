using FluentValidation;

namespace EduZim.Application.Assessments.Commands.SubmitAssessment;

public sealed class SubmitAssessmentCommandValidator : AbstractValidator<SubmitAssessmentCommand>
{
    public SubmitAssessmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AssessmentId).NotEmpty();
        RuleFor(c => c.AttemptId).NotEmpty();
        RuleFor(c => c.Answers)
            .Must(a => a.Select(x => x.QuestionId).Distinct().Count() == a.Count)
            .WithMessage("Duplicate question answers are not allowed.");
    }
}
