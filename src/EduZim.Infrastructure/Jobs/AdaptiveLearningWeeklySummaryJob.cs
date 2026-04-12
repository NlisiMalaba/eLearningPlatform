using EduZim.Application.AdaptiveLearning.Commands.RefreshWeeklySummaryCache;
using MediatR;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Weekly Hangfire entry: pre-compute adaptive weekly summaries per tenant into Redis.</summary>
public sealed class AdaptiveLearningWeeklySummaryJob
{
    private readonly IMediator _mediator;

    public AdaptiveLearningWeeklySummaryJob(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task RunAsync(CancellationToken cancellationToken = default) =>
        _mediator.Send(new RefreshAdaptiveWeeklySummaryCacheCommand(), cancellationToken);
}
