using EduZim.Application.Marketplace.DTOs;
using MediatR;

namespace EduZim.Application.Marketplace.Commands.ApproveContentPack;

public sealed record ApproveContentPackCommand(Guid ContentPackId) : IRequest<ContentPackDto>;
