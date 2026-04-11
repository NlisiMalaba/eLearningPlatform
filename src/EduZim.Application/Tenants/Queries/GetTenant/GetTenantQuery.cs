using EduZim.Application.Tenants.Models;
using MediatR;

namespace EduZim.Application.Tenants.Queries.GetTenant;

public sealed record GetTenantQuery(Guid TenantId) : IRequest<TenantDetailsDto>;
