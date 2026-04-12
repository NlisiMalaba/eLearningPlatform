using MediatR;

namespace EduZim.Application.AdaptiveLearning.Commands.RefreshWeeklySummaryCache;

public sealed record RefreshAdaptiveWeeklySummaryCacheCommand : IRequest<Unit>;
