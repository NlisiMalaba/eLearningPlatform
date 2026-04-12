using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.AdaptiveLearning.Queries.GetRecommendedPath;

public sealed record GetRecommendedPathQuery(Guid TenantId, Guid StudentId, Guid ModuleId)
    : IRequest<RecommendedPathDto>, ITenantScopedRequest;
