using MediatR;

namespace EduZim.Application.Progress.Commands.SendParentWeeklySummaries;

public sealed record SendParentWeeklySummariesCommand : IRequest<Unit>;
