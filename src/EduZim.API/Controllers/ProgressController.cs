using Asp.Versioning;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Queries.GetParentDashboard;
using EduZim.Application.Progress.Queries.GetStudentProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}")]
[Tags("Progress")]
[Authorize]
public sealed class ProgressController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ProgressController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("students/{studentId:guid}/progress")]
    [ProducesResponseType(typeof(StudentProgressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentProgress(
        Guid studentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        StudentProgressDto dto = await _mediator.Send(
                new GetStudentProgressQuery(resolvedTenantId, studentId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("parents/{parentId:guid}/dashboard")]
    [ProducesResponseType(typeof(ParentDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParentDashboard(
        Guid parentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        ParentDashboardDto dto = await _mediator.Send(
                new GetParentDashboardQuery(resolvedTenantId, parentId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
