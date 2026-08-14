using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.ZimBot.Commands.Chat;
using EduZim.Application.ZimBot.DTOs;
using EduZim.Application.ZimBot.Queries.GetInteractionLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/zimbot")]
[Tags("ZimBot")]
[Authorize]
public sealed class ZimBotController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ZimBotController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(ZimBotChatDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Chat(
        [FromBody] ZimBotChatRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        ZimBotChatDto dto = await _mediator.Send(
                new ChatCommand(
                    resolvedTenantId,
                    request.StudentId,
                    request.Message,
                    request.ModuleId,
                    request.InAssessment,
                    request.Language),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("logs/{tenantId:guid}")]
    [ProducesResponseType(typeof(ZimBotInteractionLogsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInteractionLogs(
        Guid tenantId,
        [FromQuery] Guid? studentId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        ZimBotInteractionLogsDto dto = await _mediator.Send(
                new GetInteractionLogsQuery(tenantId, studentId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
