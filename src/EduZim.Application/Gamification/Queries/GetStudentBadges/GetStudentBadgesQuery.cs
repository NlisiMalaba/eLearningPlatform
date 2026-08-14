using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using MediatR;

namespace EduZim.Application.Gamification.Queries.GetStudentBadges;

public sealed record GetStudentBadgesQuery(Guid TenantId, Guid StudentId)
    : IRequest<StudentBadgesDto>, ITenantScopedRequest;
