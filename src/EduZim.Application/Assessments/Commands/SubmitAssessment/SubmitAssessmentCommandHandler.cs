using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Assessments.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Assessments.Commands.SubmitAssessment;

public sealed class SubmitAssessmentCommandHandler : IRequestHandler<SubmitAssessmentCommand, SubmitAssessmentResultDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPublisher _publisher;
    private readonly IAssessmentBackgroundJobs _backgroundJobs;
    private readonly ILogger<SubmitAssessmentCommandHandler> _logger;

    public SubmitAssessmentCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IPublisher publisher,
        IAssessmentBackgroundJobs backgroundJobs,
        ILogger<SubmitAssessmentCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _publisher = publisher;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task<SubmitAssessmentResultDto> Handle(SubmitAssessmentCommand request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanAccessTenantScope(_currentUser, request.TenantId);
        EnsureStudentRole();

        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        (AssessmentAttempt attempt, Assessment assessment) =
            await LoadValidatedAttemptAsync(request, ct).ConfigureAwait(false);

        ValidateAnswerQuestionIds(request, assessment);

        DateTime utcNow = DateTime.UtcNow;
        Dictionary<Guid, string?> answersByQuestionId = request.Answers.ToDictionary(
            x => x.QuestionId,
            x => x.Answer);
        int timeTakenSeconds = ComputeTimeTakenSeconds(assessment, attempt, utcNow);

        SubmitAssessmentResultDto result = await AssessmentSubmitWorkflow.PersistCompletionAsync(
                _db,
                _publisher,
                _backgroundJobs,
                request.TenantId,
                attempt,
                assessment,
                answersByQuestionId,
                timeTakenSeconds,
                utcNow,
                ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Submitted assessment attempt {AttemptId} with score {Score}.",
            attempt.Id,
            result.ScorePercent);

        return result;
    }

    private void EnsureStudentRole()
    {
        if (_currentUser.Role != UserRole.Student)
        {
            throw new TenantAccessViolationException(
                "Only students can submit assessment attempts.",
                _currentUser.TenantId,
                null);
        }
    }

    private async Task<(AssessmentAttempt Attempt, Assessment Assessment)> LoadValidatedAttemptAsync(
        SubmitAssessmentCommand request,
        CancellationToken ct)
    {
        AssessmentAttempt? attempt = await _db.AssessmentAttempts
            .FirstOrDefaultAsync(
                a => a.Id == request.AttemptId && a.TenantId == request.TenantId,
                ct)
            .ConfigureAwait(false);
        if (attempt is null)
            throw new NotFoundException(nameof(AssessmentAttempt), request.AttemptId);

        if (attempt.AssessmentId != request.AssessmentId)
        {
            throw new TenantAccessViolationException(
                "The attempt does not belong to the assessment in the request path.",
                request.TenantId,
                request.AssessmentId);
        }

        if (attempt.StudentId != _currentUser.UserId)
        {
            throw new TenantAccessViolationException(
                "This attempt does not belong to the current user.",
                request.TenantId,
                request.AttemptId);
        }

        if (attempt.SubmittedAt.HasValue)
            throw new ConflictException("This assessment attempt has already been submitted.");

        if (attempt.AssessmentClassAssignmentId is null)
            throw new DomainException("This attempt is not linked to a class assignment.");

        AssessmentClassAssignment assignment = await _db.AssessmentClassAssignments
            .FirstAsync(a => a.Id == attempt.AssessmentClassAssignmentId.Value, ct)
            .ConfigureAwait(false);

        Assessment? assessment = await _db.Assessments
            .Include(a => a.Questions)
            .FirstOrDefaultAsync(a => a.Id == attempt.AssessmentId && a.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (assessment is null)
            throw new NotFoundException(nameof(Assessment), attempt.AssessmentId);

        DateTime utcNow = DateTime.UtcNow;
        if (utcNow > assignment.DueAtUtc)
            throw new DomainException("This assessment is past its due date.");

        if (assessment.TimeLimitSeconds is int limitSeconds && limitSeconds > 0)
        {
            DateTime deadline = attempt.StartedAt.AddSeconds(limitSeconds);
            if (utcNow > deadline)
                throw new DomainException("The time limit for this assessment has expired.");
        }

        return (attempt, assessment);
    }

    private static void ValidateAnswerQuestionIds(SubmitAssessmentCommand request, Assessment assessment)
    {
        HashSet<Guid> validIds = assessment.Questions.Select(q => q.Id).ToHashSet();
        foreach (SubmitAssessmentAnswerItem item in request.Answers)
        {
            if (!validIds.Contains(item.QuestionId))
                throw new DomainException("One or more answers refer to unknown questions.");
        }
    }

    private static int ComputeTimeTakenSeconds(Assessment assessment, AssessmentAttempt attempt, DateTime utcNow)
    {
        int elapsedSeconds = (int)(utcNow - attempt.StartedAt).TotalSeconds;
        if (elapsedSeconds < 0)
            elapsedSeconds = 0;

        return assessment.TimeLimitSeconds is int lim && lim > 0
            ? Math.Min(elapsedSeconds, lim)
            : elapsedSeconds;
    }
}
