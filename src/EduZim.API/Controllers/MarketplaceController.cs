using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.API.Routing;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Marketplace.Commands.ApproveAccess;
using EduZim.Application.Marketplace.Commands.ApproveContentPack;
using EduZim.Application.Marketplace.Commands.RateContentPack;
using EduZim.Application.Marketplace.Commands.RemoveContentPack;
using EduZim.Application.Marketplace.Commands.RequestAccess;
using EduZim.Application.Marketplace.Commands.SubmitContentPack;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Marketplace.Queries.BrowseContentPacks;
using EduZim.Application.Marketplace.Queries.GetContentPack;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/marketplace/packs")]
[Tags("Marketplace")]
[Authorize]
public sealed class MarketplaceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public MarketplaceController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ContentPackDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitContentPackRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        ContentPackDto dto = await _mediator.Send(
                new SubmitContentPackCommand(
                    TenantId(tenantId),
                    request.Title,
                    request.Description,
                    request.ContentItemIds),
                cancellationToken)
            .ConfigureAwait(false);

        return Created($"/{ApiRoutes.V1Marketplace}/packs/{dto.Id}", dto);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ContentPackDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Browse(
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        IReadOnlyList<ContentPackDto> packs = await _mediator.Send(
                new BrowseContentPacksQuery(TenantId(tenantId)),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(packs);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContentPackDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        ContentPackDetailDto dto = await _mediator.Send(
                new GetContentPackQuery(TenantId(tenantId), id),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "PlatformAdmin")]
    [ProducesResponseType(typeof(ContentPackDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApprovePack(Guid id, CancellationToken cancellationToken)
    {
        ContentPackDto dto = await _mediator.Send(new ApproveContentPackCommand(id), cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/request-access")]
    [ProducesResponseType(typeof(RequestAccessResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestAccess(
        Guid id,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        Guid requestId = await _mediator.Send(
                new RequestAccessCommand(TenantId(tenantId), id),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(new RequestAccessResponse { AccessRequestId = requestId });
    }

    [HttpPost("{id:guid}/approve-access")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApproveAccess(
        Guid id,
        [FromBody] ApproveAccessRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        await _mediator.Send(
                new ApproveAccessCommand(TenantId(tenantId), id, request.RequestingTenantId),
                cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{id:guid}/rate")]
    [ProducesResponseType(typeof(ContentPackDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Rate(
        Guid id,
        [FromBody] RateContentPackRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        ContentPackDto dto = await _mediator.Send(
                new RateContentPackCommand(TenantId(tenantId), id, request.Rating, request.Review),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "PlatformAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveContentPackCommand(id), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    private IActionResult? TenantError(Guid? tenantId) =>
        TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId);

    private Guid TenantId(Guid? tenantId) =>
        TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
}
