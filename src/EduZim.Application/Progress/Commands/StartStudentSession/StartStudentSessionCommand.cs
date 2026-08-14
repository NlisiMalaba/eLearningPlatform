using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using MediatR;

namespace EduZim.Application.Progress.Commands.StartStudentSession;

public sealed record StartStudentSessionCommand(Guid TenantId, Guid StudentId)
    : IRequest<StudentSessionDto>, ITenantScopedRequest;
