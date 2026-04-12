using EduZim.Application.Assessments.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Assessments.Commands.AutoSubmitAssessment;

public sealed class AutoSubmitAssessmentCommandHandler : IRequestHandler<AutoSubmitAssessmentCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly IPublisher _publisher;
    private readonly IAssessmentBackgroundJobs _backgroundJobs;
    private readonly ILogger<AutoSubmitAssessmentCommandHandler> _logger;

    public AutoSubmitAssessmentCommandHandler(
        IEduZimDbContext db,
        IPublisher publisher,
        IAssessmentBackgroundJobs backgroundJobs,
        ILogger<AutoSubmitAssessmentCommandHandler> logger)
    {
        _db = db;
        _publisher = publisher;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task<Unit> Handle(AutoSubmitAssessmentCommand request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        AssessmentAttempt? attempt = await _db.AssessmentAttempts
            .FirstOrDefaultAsync(
                a => a.Id == request.AttemptId && a.TenantId == request.TenantId,
                ct)
            .ConfigureAwait(false);
        if (attempt is null)
        {
            _logger.LogWarning(
                "Auto-submit skipped: attempt {AttemptId} not found for tenant {TenantId}.",
                request.AttemptId,
                request.TenantId);
            return Unit.Value;
        }

        if (attempt.SubmittedAt.HasValue)
            return Unit.Value;

        Assessment? assessment = await _db.Assessments
            .Include(a => a.Questions)
            .FirstOrDefaultAsync(a => a.Id == attempt.AssessmentId && a.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (assessment is null)
            throw new NotFoundException(nameof(Assessment), attempt.AssessmentId);

        Dictionary<Guid, string?> emptyAnswers = assessment.Questions.ToDictionary(q => q.Id, _ => (string?)null);

        DateTime utcNow = DateTime.UtcNow;
        int elapsedSeconds = (int)(utcNow - attempt.StartedAt).TotalSeconds;
        if (elapsedSeconds < 0)
            elapsedSeconds = 0;

        int timeTakenSeconds = assessment.TimeLimitSeconds is int lim && lim > 0
            ? Math.Min(elapsedSeconds, lim)
            : elapsedSeconds;

        await AssessmentSubmitWorkflow.PersistCompletionAsync(
                _db,
                _publisher,
                _backgroundJobs,
                request.TenantId,
                attempt,
                assessment,
                emptyAnswers,
                timeTakenSeconds,
                utcNow,
                ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Auto-submitted timed assessment attempt {AttemptId} for tenant {TenantId}.",
            request.AttemptId,
            request.TenantId);

        return Unit.Value;
    }
}
