using MediatR;

namespace EduZim.Application.Marketplace.Commands.RemoveContentPack;

public sealed record RemoveContentPackCommand(Guid ContentPackId) : IRequest<Unit>;
