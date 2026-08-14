using EduZim.Application.Common.Interfaces;
using EduZim.Application.Marketplace.DTOs;
using MediatR;

namespace EduZim.Application.Marketplace.Commands.RateContentPack;

public sealed record RateContentPackCommand(
    Guid TenantId,
    Guid ContentPackId,
    int Rating,
    string? Review) : IRequest<ContentPackDto>, ITenantScopedRequest;
