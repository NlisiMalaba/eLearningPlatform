using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using MediatR;

namespace EduZim.Application.Gamification.Queries.GetLeaderboard;

public sealed record GetLeaderboardQuery(Guid TenantId, int Limit = 50)
    : IRequest<LeaderboardDto>, ITenantScopedRequest;
