using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using MediatR;

namespace EduZim.Application.Progress.Queries.GetStudentProgress;

public sealed record GetStudentProgressQuery(Guid TenantId, Guid StudentId)
    : IRequest<StudentProgressDto>, ITenantScopedRequest;
