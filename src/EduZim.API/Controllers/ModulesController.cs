using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.API.Routing;
using EduZim.Application.Content.Commands.CreateModule;
using EduZim.Application.Content.Queries.GetModuleById;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/modules")]
[Tags("Modules")]
[Authorize]
public sealed class ModulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ModulesController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateModuleResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateModule(
        [FromBody] CreateModuleRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (ValidateTenantQuery(tenantId) is { } err)
            return err;

        var resolvedTenantId = ResolveTenantId(tenantId);
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, resolvedTenantId);

        var id = await _mediator.Send(
            new CreateModuleCommand(
                resolvedTenantId,
                request.Title,
                request.Grade,
                request.Subject,
                request.SequenceOrder,
                request.IsRequired),
            cancellationToken);

        return Created(
            $"/{ApiRoutes.V1Modules}/{id}",
            new CreateModuleResponse { ModuleId = id });
    }

    [HttpGet("{moduleId:guid}")]
    [ProducesResponseType(typeof(ModuleDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModule(
        Guid moduleId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (ValidateTenantQuery(tenantId) is { } err)
            return err;

        var resolvedTenantId = ResolveTenantId(tenantId);
        TenantAccessHelper.EnsureCanAccessTenantScope(_currentUser, resolvedTenantId);

        var dto = await _mediator.Send(new GetModuleByIdQuery(resolvedTenantId, moduleId), cancellationToken);
        return Ok(dto);
    }

    private IActionResult? ValidateTenantQuery(Guid? tenantId)
    {
        if (_currentUser.Role == UserRole.PlatformAdmin)
        {
            if (tenantId is null)
            {
                return Problem(
                    title: "Tenant required",
                    detail: "Provide tenantId (query) when using platform administrator credentials.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return null;
        }

        if (_currentUser.TenantId is null)
            return Forbid();

        if (tenantId.HasValue && tenantId.Value != _currentUser.TenantId.Value)
        {
            return Problem(
                title: "Tenant mismatch",
                detail: "tenantId does not match the authenticated user's tenant.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        return null;
    }

    private Guid ResolveTenantId(Guid? tenantId)
    {
        return _currentUser.Role == UserRole.PlatformAdmin
            ? tenantId!.Value
            : _currentUser.TenantId!.Value;
    }
}
