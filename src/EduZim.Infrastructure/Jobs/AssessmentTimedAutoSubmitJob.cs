using EduZim.Application.Assessments.Commands.AutoSubmitAssessment;
using MediatR;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Hangfire entry point: auto-submit timed assessments when the time window elapses.</summary>
public sealed class AssessmentTimedAutoSubmitJob
{
    private readonly IMediator _mediator;

    public AssessmentTimedAutoSubmitJob(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task RunAsync(Guid tenantId, Guid attemptId) =>
        _mediator.Send(new AutoSubmitAssessmentCommand(tenantId, attemptId), CancellationToken.None);
}
