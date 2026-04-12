using Asp.Versioning;
using EduZim.Application.AdaptiveLearning.Commands.RefreshStudentAdaptiveCaches;
using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.AdaptiveLearning.Queries.GetRecommendedPath;
using EduZim.Application.AdaptiveLearning.Queries.GetWeeklySummary;
using EduZim.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/adaptive")]
[Tags("Adaptive")]
[Authorize]
public sealed class AdaptiveController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AdaptiveController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("{studentId:guid}/path")]
    [ProducesResponseType(typeof(RecommendedPathDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendedPath(
        Guid studentId,
        [FromQuery] Guid moduleId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);

        RecommendedPathDto dto = await _mediator.Send(
                new GetRecommendedPathQuery(resolvedTenantId, studentId, moduleId),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(dto);
    }

    [HttpGet("{studentId:guid}/summary")]
    [ProducesResponseType(typeof(WeeklyAdaptiveSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeeklySummary(
        Guid studentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);

        WeeklyAdaptiveSummaryDto dto = await _mediator.Send(
                new GetWeeklySummaryQuery(resolvedTenantId, studentId),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(dto);
    }

    [HttpPost("{studentId:guid}/update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RefreshAdaptiveCaches(
        Guid studentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);

        await _mediator.Send(
                new RefreshStudentAdaptiveCachesCommand(resolvedTenantId, studentId),
                cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }
}
