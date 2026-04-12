using EduZim.Domain.Enums;
using FluentValidation;

namespace EduZim.Application.Assessments.Commands.CreateAssessment;

public sealed class CreateAssessmentCommandValidator : AbstractValidator<CreateAssessmentCommand>
{
    public CreateAssessmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ModuleId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(256);
        RuleFor(c => c.PassingScorePercent).InclusiveBetween(0, 100);
        RuleFor(c => c.TimeLimitSeconds).InclusiveBetween(1, 86400).When(c => c.TimeLimitSeconds.HasValue);
        RuleFor(c => c.Questions).NotEmpty();
        RuleForEach(c => c.Questions).ChildRules(q =>
        {
            q.RuleFor(x => x.Text).NotEmpty().MaximumLength(4000);
            q.RuleFor(x => x.Points).GreaterThan(0);
            q.When(
                x => x.Type == QuestionType.MultipleChoice,
                () =>
                {
                    q.RuleFor(x => x.OptionTexts).NotNull();
                    q.RuleFor(x => x.OptionTexts!.Count).GreaterThanOrEqualTo(2);
                    q.RuleFor(x => x.CorrectOptionIndex).NotNull();
                    q.RuleFor(x => x)
                        .Must(
                            x => x.CorrectOptionIndex is not null
                                && x.OptionTexts is not null
                                && x.CorrectOptionIndex.Value < x.OptionTexts.Count)
                        .WithMessage("Correct option index is out of range.");
                });
            q.When(
                x => x.Type == QuestionType.TrueFalse,
                () =>
                {
                    q.RuleFor(x => x.CorrectOptionIndex).NotNull().InclusiveBetween(0, 1);
                    q.RuleFor(x => x.OptionTexts)
                        .Must(ot => ot is null || ot.Count == 2)
                        .WithMessage("True/false questions must have zero custom options or exactly two.");
                });
            q.When(
                x => x.Type == QuestionType.ShortAnswer,
                () =>
                {
                    q.RuleFor(x => x.CorrectShortAnswer).NotEmpty().MaximumLength(2000);
                });
        });
    }
}
