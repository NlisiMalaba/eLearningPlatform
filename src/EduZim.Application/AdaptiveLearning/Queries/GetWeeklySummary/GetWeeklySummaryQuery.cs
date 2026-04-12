using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.AdaptiveLearning.Queries.GetWeeklySummary;

public sealed record GetWeeklySummaryQuery(Guid TenantId, Guid StudentId)
    : IRequest<WeeklyAdaptiveSummaryDto>, ITenantScopedRequest;
