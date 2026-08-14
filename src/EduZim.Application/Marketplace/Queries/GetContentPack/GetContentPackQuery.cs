using EduZim.Application.Common.Interfaces;
using EduZim.Application.Marketplace.DTOs;
using MediatR;

namespace EduZim.Application.Marketplace.Queries.GetContentPack;

public sealed record GetContentPackQuery(Guid TenantId, Guid ContentPackId)
    : IRequest<ContentPackDetailDto>, ITenantScopedRequest;
