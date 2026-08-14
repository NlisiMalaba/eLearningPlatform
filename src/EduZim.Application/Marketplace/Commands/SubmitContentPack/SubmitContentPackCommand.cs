using EduZim.Application.Common.Interfaces;
using EduZim.Application.Marketplace.DTOs;
using MediatR;

namespace EduZim.Application.Marketplace.Commands.SubmitContentPack;

public sealed record SubmitContentPackCommand(
    Guid TenantId,
    string Title,
    string Description,
    IReadOnlyList<Guid> ContentItemIds) : IRequest<ContentPackDto>, ITenantScopedRequest;
