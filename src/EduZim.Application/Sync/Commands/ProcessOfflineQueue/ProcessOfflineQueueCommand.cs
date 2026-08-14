using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.DTOs;
using MediatR;

namespace EduZim.Application.Sync.Commands.ProcessOfflineQueue;

public sealed record ProcessOfflineQueueCommand(
    Guid TenantId,
    Guid StudentId,
    IReadOnlyList<OfflineSyncItemDto> Items)
    : IRequest<ProcessOfflineQueueResultDto>, ITenantScopedRequest;
