using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using MediatR;

namespace EduZim.Application.Progress.Commands.RecordStudentSessionHeartbeat;

public sealed record RecordStudentSessionHeartbeatCommand(Guid TenantId, Guid StudentId)
    : IRequest<StudentSessionDto>, ITenantScopedRequest;
