using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.API.Routing;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.LiveClassrooms.Commands.EndSession;
using EduZim.Application.LiveClassrooms.Commands.ScheduleSession;
using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Application.LiveClassrooms.Queries.GetAttendance;
using EduZim.Application.LiveClassrooms.Queries.GetJoinToken;
using EduZim.Application.LiveClassrooms.Queries.GetRecordingUrl;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/classrooms")]
[Tags("Live Classrooms")]
[Authorize]
public sealed class ClassroomsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ClassroomsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ScheduleClassroomResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Schedule(
        [FromBody] ScheduleClassroomRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        Guid sessionId = await _mediator.Send(
                new ScheduleSessionCommand(
                    resolvedTenantId,
                    request.SchoolClassId,
                    request.StartAtUtc,
                    request.DurationMinutes),
                cancellationToken)
            .ConfigureAwait(false);

        return Created(
            $"/{ApiRoutes.V1Classrooms}/{sessionId}",
            new ScheduleClassroomResponse { SessionId = sessionId });
    }

    [HttpGet("{id:guid}/join")]
    [ProducesResponseType(typeof(JoinTokenDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Join(
        Guid id,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        JoinTokenDto dto = await _mediator.Send(
                new GetJoinTokenQuery(resolvedTenantId, id),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/end")]
    [ProducesResponseType(typeof(SessionAttendanceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> End(
        Guid id,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        SessionAttendanceDto dto = await _mediator.Send(
                new EndSessionCommand(resolvedTenantId, id),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("{id:guid}/attendance")]
    [ProducesResponseType(typeof(SessionAttendanceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendance(
        Guid id,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        SessionAttendanceDto dto = await _mediator.Send(
                new GetAttendanceQuery(resolvedTenantId, id),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("{id:guid}/recording")]
    [ProducesResponseType(typeof(ClassroomRecordingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecording(
        Guid id,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        string? url = await _mediator.Send(
                new GetRecordingUrlQuery(resolvedTenantId, id),
                cancellationToken)
            .ConfigureAwait(false);
        if (url is null)
            return NotFound();

        return Ok(new ClassroomRecordingResponse { Url = url });
    }
}
