using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Identity.Commands.UpdateFontSizePreference;
using EduZim.Application.Identity.DTOs;
using EduZim.Application.Notifications.Commands.UpdateNotificationPreferences;
using EduZim.Application.Notifications.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public UsersController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPut("{userId:guid}/notification-preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNotificationPreferences(
        Guid userId,
        [FromBody] UpdateNotificationPreferencesRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        NotificationPreferencesDto dto = await _mediator.Send(
                new UpdateNotificationPreferencesCommand(resolvedTenantId, userId, request.Preferences),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPut("{userId:guid}/font-size")]
    [ProducesResponseType(typeof(FontSizePreferenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateFontSize(
        Guid userId,
        [FromBody] UpdateFontSizePreferenceRequest request,
        CancellationToken cancellationToken)
    {
        FontSizePreferenceDto dto = await _mediator.Send(
                new UpdateFontSizePreferenceCommand(userId, request.FontSize),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
