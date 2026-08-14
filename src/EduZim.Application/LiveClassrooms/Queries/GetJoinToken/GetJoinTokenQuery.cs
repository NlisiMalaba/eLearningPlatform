using EduZim.Application.Common.Interfaces;
using EduZim.Application.LiveClassrooms.DTOs;
using MediatR;

namespace EduZim.Application.LiveClassrooms.Queries.GetJoinToken;

public sealed record GetJoinTokenQuery(Guid TenantId, Guid SessionId)
    : IRequest<JoinTokenDto>, ITenantScopedRequest;
