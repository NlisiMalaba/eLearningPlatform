using EduZim.Application.Assessments.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Application.Assessments.Services;

public static class AssessmentGrading
{
    public static (int ScorePercent, List<QuestionFeedbackDto> Feedback) Compute(
        IReadOnlyList<Question> questions,
        IReadOnlyDictionary<Guid, string?> answersByQuestionId)
    {
        int earned = 0;
        int totalPoints = 0;
        var feedback = new List<QuestionFeedbackDto>();
        foreach (Question q in questions)
        {
            totalPoints += q.Points;
            answersByQuestionId.TryGetValue(q.Id, out string? raw);
            bool ok = IsCorrect(q, raw);
            if (ok)
                earned += q.Points;

            string message = ok ? "Correct." : "Incorrect.";
            feedback.Add(new QuestionFeedbackDto(q.Id, ok, message));
        }

        int percent = totalPoints == 0
            ? 0
            : (int)Math.Round(100.0 * earned / totalPoints, MidpointRounding.AwayFromZero);
        return (percent, feedback);
    }

    private static bool IsCorrect(Question q, string? raw)
    {
        string? answer = raw?.Trim();
        return q.Type switch
        {
            QuestionType.MultipleChoice or QuestionType.TrueFalse =>
                !string.IsNullOrEmpty(answer)
                && string.Equals(answer, q.CorrectAnswer?.Trim(), StringComparison.Ordinal),
            QuestionType.ShortAnswer =>
                !string.IsNullOrEmpty(answer)
                && !string.IsNullOrEmpty(q.CorrectAnswer)
                && string.Equals(answer, q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }
}
