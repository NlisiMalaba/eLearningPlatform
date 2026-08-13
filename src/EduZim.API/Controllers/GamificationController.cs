using Asp.Versioning;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using EduZim.Application.Gamification.Queries.GetLeaderboard;
using EduZim.Application.Gamification.Queries.GetStudentBadges;
using EduZim.Application.Gamification.Queries.GetStudentPoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/gamification")]
[Tags("Gamification")]
[Authorize]
public sealed class GamificationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public GamificationController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("{studentId:guid}/points")]
    [ProducesResponseType(typeof(StudentPointsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentPoints(
        Guid studentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        StudentPointsDto dto = await _mediator.Send(
                new GetStudentPointsQuery(resolvedTenantId, studentId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("{studentId:guid}/badges")]
    [ProducesResponseType(typeof(StudentBadgesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentBadges(
        Guid studentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        StudentBadgesDto dto = await _mediator.Send(
                new GetStudentBadgesQuery(resolvedTenantId, studentId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("leaderboard/{tenantId:guid}")]
    [ProducesResponseType(typeof(LeaderboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(
        Guid tenantId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        LeaderboardDto dto = await _mediator.Send(
                new GetLeaderboardQuery(tenantId, limit),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
