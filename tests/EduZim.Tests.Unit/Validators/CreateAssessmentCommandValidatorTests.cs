using EduZim.Application.Assessments.Commands.CreateAssessment;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Unit.Validators;

public sealed class CreateAssessmentCommandValidatorTests
{
    private readonly CreateAssessmentCommandValidator _validator = new();

    [Fact]
    public async Task Valid_multiple_choice_passes()
    {
        Guid tenantId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        var command = new CreateAssessmentCommand(
            tenantId,
            moduleId,
            "Test",
            TimeLimitSeconds: 120,
            PassingScorePercent: 60,
            new List<CreateAssessmentQuestionItem>
            {
                new(
                    QuestionType.MultipleChoice,
                    "Pick one",
                    5,
                    new[] { "A", "B", "C" },
                    CorrectOptionIndex: 1,
                    CorrectShortAnswer: null),
            });

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Multiple_choice_index_out_of_range_fails()
    {
        Guid tenantId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        var command = new CreateAssessmentCommand(
            tenantId,
            moduleId,
            "Test",
            null,
            60,
            new List<CreateAssessmentQuestionItem>
            {
                new(
                    QuestionType.MultipleChoice,
                    "Pick one",
                    5,
                    new[] { "A", "B" },
                    CorrectOptionIndex: 5,
                    CorrectShortAnswer: null),
            });

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }
}
