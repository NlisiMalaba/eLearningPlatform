using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Assessments.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Unit.Handlers;

public sealed class AssessmentGradingTests
{
    [Fact]
    public void Compute_AllCorrect_Returns100Percent()
    {
        Guid q1 = Guid.NewGuid();
        var questions = new List<Question>
        {
            new()
            {
                Id = q1,
                TenantId = Guid.NewGuid(),
                AssessmentId = Guid.NewGuid(),
                Type = QuestionType.ShortAnswer,
                Text = "Capital of France?",
                Points = 10,
                CorrectAnswer = "Paris",
            },
        };

        Dictionary<Guid, string?> answers = new() { { q1, "paris" } };

        (int percent, List<QuestionFeedbackDto> feedback) = AssessmentGrading.Compute(questions, answers);

        Assert.Equal(100, percent);
        Assert.Single(feedback);
        Assert.True(feedback[0].IsCorrect);
    }

    [Fact]
    public void Compute_PartialCredit_ReturnsRoundedPercent()
    {
        Guid q1 = Guid.NewGuid();
        Guid q2 = Guid.NewGuid();
        var questions = new List<Question>
        {
            new()
            {
                Id = q1,
                TenantId = Guid.NewGuid(),
                AssessmentId = Guid.NewGuid(),
                Type = QuestionType.ShortAnswer,
                Text = "A",
                Points = 1,
                CorrectAnswer = "a",
            },
            new()
            {
                Id = q2,
                TenantId = Guid.NewGuid(),
                AssessmentId = Guid.NewGuid(),
                Type = QuestionType.ShortAnswer,
                Text = "B",
                Points = 1,
                CorrectAnswer = "b",
            },
        };

        Dictionary<Guid, string?> answers = new() { { q1, "a" }, { q2, "wrong" } };

        (int percent, _) = AssessmentGrading.Compute(questions, answers);

        Assert.Equal(50, percent);
    }
}
