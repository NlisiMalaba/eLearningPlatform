using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Content.Queries.GetModuleById;

public sealed record GetModuleByIdQuery(Guid TenantId, Guid ModuleId)
    : IRequest<ModuleDetailDto>, ITenantScopedRequest;
