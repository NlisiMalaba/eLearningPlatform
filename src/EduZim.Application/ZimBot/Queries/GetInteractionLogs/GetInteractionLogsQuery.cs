using EduZim.Application.Common.Interfaces;
using EduZim.Application.ZimBot.DTOs;
using MediatR;

namespace EduZim.Application.ZimBot.Queries.GetInteractionLogs;

public sealed record GetInteractionLogsQuery(Guid TenantId, Guid? StudentId)
    : IRequest<ZimBotInteractionLogsDto>, ITenantScopedRequest;
