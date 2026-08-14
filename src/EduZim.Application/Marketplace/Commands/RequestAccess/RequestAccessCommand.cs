using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Marketplace.Commands.RequestAccess;

public sealed record RequestAccessCommand(Guid TenantId, Guid ContentPackId)
    : IRequest<Guid>, ITenantScopedRequest;
