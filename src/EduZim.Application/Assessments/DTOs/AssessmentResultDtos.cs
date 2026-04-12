namespace EduZim.Application.Assessments.DTOs;

public sealed record SubmitAssessmentResultDto(
    Guid AttemptId,
    int ScorePercent,
    int TimeTakenSeconds,
    DateTime SubmittedAtUtc,
    IReadOnlyList<QuestionFeedbackDto> QuestionFeedback);

public sealed record QuestionFeedbackDto(Guid QuestionId, bool IsCorrect, string Feedback);

public sealed record ClassAssessmentResultsDto(
    Guid SchoolClassId,
    string SchoolClassName,
    int EnrolledStudentCount,
    int CompletedCount,
    int CompletionRatePercent,
    double? AverageTimeTakenSecondsAmongCompleters,
    IReadOnlyList<StudentAssessmentResultRowDto> Students);

public sealed record StudentAssessmentResultRowDto(
    Guid StudentId,
    int? ScorePercent,
    bool HasCompleted,
    int? TimeTakenSeconds);

public sealed record BeginAssessmentSessionResultDto(Guid AttemptId);
