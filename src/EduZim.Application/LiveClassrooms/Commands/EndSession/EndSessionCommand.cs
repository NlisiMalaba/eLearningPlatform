using EduZim.Application.Common.Interfaces;
using EduZim.Application.LiveClassrooms.DTOs;
using MediatR;

namespace EduZim.Application.LiveClassrooms.Commands.EndSession;

public sealed record EndSessionCommand(Guid TenantId, Guid SessionId)
    : IRequest<SessionAttendanceDto>, ITenantScopedRequest;
