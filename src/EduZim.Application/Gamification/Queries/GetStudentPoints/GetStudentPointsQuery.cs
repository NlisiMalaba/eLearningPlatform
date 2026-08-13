using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using MediatR;

namespace EduZim.Application.Gamification.Queries.GetStudentPoints;

public sealed record GetStudentPointsQuery(Guid TenantId, Guid StudentId)
    : IRequest<StudentPointsDto>, ITenantScopedRequest;
