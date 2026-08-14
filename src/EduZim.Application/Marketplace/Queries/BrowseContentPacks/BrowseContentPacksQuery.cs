using EduZim.Application.Common.Interfaces;
using EduZim.Application.Marketplace.DTOs;
using MediatR;

namespace EduZim.Application.Marketplace.Queries.BrowseContentPacks;

public sealed record BrowseContentPacksQuery(Guid TenantId)
    : IRequest<IReadOnlyList<ContentPackDto>>, ITenantScopedRequest;
