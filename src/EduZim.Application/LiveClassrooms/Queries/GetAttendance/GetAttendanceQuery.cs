using EduZim.Application.Common.Interfaces;
using EduZim.Application.LiveClassrooms.DTOs;
using MediatR;

namespace EduZim.Application.LiveClassrooms.Queries.GetAttendance;

public sealed record GetAttendanceQuery(Guid TenantId, Guid SessionId)
    : IRequest<SessionAttendanceDto>, ITenantScopedRequest;
