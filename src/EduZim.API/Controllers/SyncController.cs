using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.Commands.ProcessOfflineQueue;
using EduZim.Application.Sync.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sync")]
[Tags("Sync")]
[Authorize]
public sealed class SyncController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public SyncController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadOfflineQueueResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upload(
        [FromBody] UploadOfflineQueueRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        ProcessOfflineQueueResultDto result = await _mediator.Send(
                new ProcessOfflineQueueCommand(
                    resolvedTenantId,
                    request.StudentId,
                    MapItems(request.Items ?? [])),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(
            new UploadOfflineQueueResponse
            {
                AcceptedCount = result.AcceptedCount,
                SyncedCount = result.SyncedCount,
                ConflictedCount = result.ConflictedCount,
            });
    }

    private static IReadOnlyList<OfflineSyncItemDto> MapItems(IReadOnlyList<UploadOfflineQueueItemRequest> items)
    {
        return items.Select(MapItem).ToList();
    }

    private static OfflineSyncItemDto MapItem(UploadOfflineQueueItemRequest item)
    {
        UploadOfflineQueuePayloadRequest payload = item.Payload;
        return new OfflineSyncItemDto(
            item.ClientId,
            item.LocalTimestamp,
            new OfflineSyncPayloadDto(
                payload.Kind,
                payload.ModuleId,
                payload.IsCompleted,
                payload.CompletedAt,
                payload.TimeOnTaskSeconds,
                payload.AssessmentId,
                payload.AttemptId,
                payload.ScorePercent,
                payload.TimeTakenSeconds,
                payload.SubmittedAt));
    }
}
