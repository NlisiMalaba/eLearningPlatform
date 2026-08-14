using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using MediatR;

namespace EduZim.Application.Progress.Queries.GetParentDashboard;

public sealed record GetParentDashboardQuery(Guid TenantId, Guid ParentId)
    : IRequest<ParentDashboardDto>, ITenantScopedRequest;
