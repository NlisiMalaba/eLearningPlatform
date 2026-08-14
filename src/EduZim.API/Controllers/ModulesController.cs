using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.API.Routing;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Commands.CreateModule;
using EduZim.Application.Content.Commands.SetModuleContentItems;
using EduZim.Application.Content.Queries.GetModuleById;
using EduZim.Application.Content.Queries.ListModules;
using EduZim.Application.Tenants;
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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ModuleListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] Guid? tenantId, CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        IReadOnlyList<ModuleListItemDto> items = await _mediator.Send(
                new ListModulesQuery(TenantId(tenantId)),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateModuleResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateModule(
        [FromBody] CreateModuleRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantId(tenantId);
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, resolvedTenantId);
        Guid id = await _mediator.Send(
                new CreateModuleCommand(
                    resolvedTenantId,
                    request.Title,
                    request.Grade,
                    request.Subject,
                    request.SequenceOrder,
                    request.IsRequired),
                cancellationToken)
            .ConfigureAwait(false);
        return Created($"/{ApiRoutes.V1Modules}/{id}", new CreateModuleResponse { ModuleId = id });
    }

    [HttpGet("{moduleId:guid}")]
    [ProducesResponseType(typeof(ModuleDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModule(
        Guid moduleId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        ModuleDetailDto dto = await _mediator.Send(
                new GetModuleByIdQuery(TenantId(tenantId), moduleId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPut("{moduleId:guid}/content-items")]
    [ProducesResponseType(typeof(ModuleDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetContentItems(
        Guid moduleId,
        [FromBody] SetModuleContentItemsRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        ModuleDetailDto dto = await _mediator.Send(
                new SetModuleContentItemsCommand(TenantId(tenantId), moduleId, request.ContentItemIds),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    private IActionResult? TenantError(Guid? tenantId) =>
        TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId);

    private Guid TenantId(Guid? tenantId) =>
        TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
}
