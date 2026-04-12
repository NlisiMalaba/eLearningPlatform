using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.AdaptiveLearning.Queries.GetAdjustedDifficulty;

public sealed record GetAdjustedDifficultyQuery(Guid TenantId, Guid StudentId, Guid ModuleId)
    : IRequest<AdjustedDifficultyDto>, ITenantScopedRequest;
