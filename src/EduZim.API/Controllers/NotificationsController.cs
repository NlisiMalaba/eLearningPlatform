using Asp.Versioning;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.MarkNotificationRead;
using EduZim.Application.Notifications.DTOs;
using EduZim.Application.Notifications.Queries.GetInAppNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/notifications")]
[Tags("Notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(InAppNotificationsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInAppNotifications(
        Guid userId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        InAppNotificationsDto dto = await _mediator.Send(
                new GetInAppNotificationsQuery(resolvedTenantId, userId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(
        Guid id,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        await _mediator.Send(
                new MarkNotificationReadCommand(resolvedTenantId, id),
                cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }
}
