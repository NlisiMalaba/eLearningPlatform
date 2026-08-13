using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.DTOs;
using MediatR;

namespace EduZim.Application.Sync.Commands.ResolveConflict;

public sealed record ResolveConflictCommand(
    Guid TenantId,
    Guid StudentId,
    Guid QueueItemId,
    DateTime LocalTimestamp,
    OfflineSyncPayloadDto Payload)
    : IRequest<ResolveConflictResultDto>, ITenantScopedRequest;
