using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Events;
using MediatR;

namespace EduZim.Application.Assessments.Services;
internal static class AssessmentSubmitWorkflow
{
    public static async Task<SubmitAssessmentResultDto> PersistCompletionAsync(
        IEduZimDbContext db,
        IPublisher publisher,
        IAssessmentBackgroundJobs backgroundJobs,
        Guid tenantId,
        AssessmentAttempt attempt,
        Assessment assessment,
        IReadOnlyDictionary<Guid, string?> answersByQuestionId,
        int timeTakenSeconds,
        DateTime submittedAtUtc,
        CancellationToken ct)
    {
        IReadOnlyList<Question> questions = assessment.Questions.ToList();
        (int scorePercent, List<QuestionFeedbackDto> feedback) =
            AssessmentGrading.Compute(questions, answersByQuestionId);

        attempt.ScorePercent = scorePercent;
        attempt.TimeTakenSeconds = timeTakenSeconds;
        attempt.SubmittedAt = submittedAtUtc;
        attempt.UpdatedAt = submittedAtUtc;
        string? jobId = attempt.TimedAutoSubmitHangfireJobId;
        attempt.TimedAutoSubmitHangfireJobId = null;
        backgroundJobs.TryCancelJob(jobId);

        foreach (Question q in questions)
        {
            answersByQuestionId.TryGetValue(q.Id, out string? a);
            await db.AnswerRecords.AddAsync(
                    new AnswerRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        AssessmentAttemptId = attempt.Id,
                        QuestionId = q.Id,
                        Answer = a,
                    },
                    ct)
                .ConfigureAwait(false);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await publisher
            .Publish(
                new AssessmentSubmittedNotification(
                    attempt.StudentId,
                    assessment.Id,
                    attempt.Id,
                    tenantId,
                    scorePercent),
                ct)
            .ConfigureAwait(false);

        return new SubmitAssessmentResultDto(
            attempt.Id,
            scorePercent,
            timeTakenSeconds,
            submittedAtUtc,
            feedback);
    }
}
