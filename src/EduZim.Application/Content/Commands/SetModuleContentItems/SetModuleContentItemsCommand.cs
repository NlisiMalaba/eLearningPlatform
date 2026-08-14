using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Queries.GetModuleById;
using MediatR;

namespace EduZim.Application.Content.Commands.SetModuleContentItems;

public sealed record SetModuleContentItemsCommand(
    Guid TenantId,
    Guid ModuleId,
    IReadOnlyList<Guid> ContentItemIds) : IRequest<ModuleDetailDto>, ITenantScopedRequest;
