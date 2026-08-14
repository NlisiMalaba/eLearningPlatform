using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using MediatR;

namespace EduZim.Application.Progress.Commands.ResumeStudentSession;

public sealed record ResumeStudentSessionCommand(Guid TenantId, Guid StudentId)
    : IRequest<StudentSessionDto>, ITenantScopedRequest;
