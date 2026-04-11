using EduZim.Application.Tenants.Models;
using MediatR;

namespace EduZim.Application.Tenants.Queries.GetTenantDashboard;

public sealed record GetTenantDashboardQuery(Guid TenantId) : IRequest<TenantDashboardDto>;
