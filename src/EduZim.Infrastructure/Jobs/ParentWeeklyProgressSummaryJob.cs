using EduZim.Application.Progress.Commands.SendParentWeeklySummaries;
using MediatR;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Weekly Hangfire entry: email + SMS progress summaries for linked parents (requirement 10.4).</summary>
public sealed class ParentWeeklyProgressSummaryJob
{
    private readonly IMediator _mediator;

    public ParentWeeklyProgressSummaryJob(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task RunAsync(CancellationToken cancellationToken = default) =>
        _mediator.Send(new SendParentWeeklySummariesCommand(), cancellationToken);
}
