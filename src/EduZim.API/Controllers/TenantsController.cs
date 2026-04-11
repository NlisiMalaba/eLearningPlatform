using EduZim.API.Contracts;
using EduZim.Application.Tenants.Commands.GenerateInviteCode;
using EduZim.Application.Tenants.Commands.ProvisionTenant;
using EduZim.Application.Tenants.Commands.UpdateBranding;
using EduZim.Application.Tenants.Models;
using EduZim.Application.Tenants.Queries.GetTenant;
using EduZim.Application.Tenants.Queries.GetTenantDashboard;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[Route("tenants")]
[Authorize]
public sealed class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "PlatformAdmin")]
    [ProducesResponseType(typeof(ProvisionTenantResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> ProvisionTenant([FromBody] ProvisionTenantRequest request, CancellationToken cancellationToken)
    {
        BrandingSettings? branding = null;
        if (request.InitialBranding is { } ib)
        {
            branding = new BrandingSettings
            {
                SchoolName = ib.SchoolName ?? request.Name,
                PrimaryColour = ib.PrimaryColour ?? "#1976D2",
                LogoUrl = ib.LogoUrl,
                SsoAuthorizationEndpoint = ib.SsoAuthorizationEndpoint,
            };
        }

        var id = await _mediator.Send(
            new ProvisionTenantCommand(request.Name, request.Tier, branding),
            cancellationToken);

        return Created(
            $"/tenants/{id}",
            new ProvisionTenantResponse { TenantId = id });
    }

    [HttpGet("{tenantId:guid}")]
    [ProducesResponseType(typeof(TenantDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetTenantQuery(tenantId), cancellationToken);
        return Ok(dto);
    }

    [HttpPut("{tenantId:guid}/branding")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateBranding(
        Guid tenantId,
        [FromBody] UpdateBrandingRequest request,
        CancellationToken cancellationToken)
    {
        var branding = new BrandingSettings
        {
            SchoolName = request.SchoolName,
            PrimaryColour = request.PrimaryColour,
            LogoUrl = request.LogoUrl,
            SsoAuthorizationEndpoint = request.SsoAuthorizationEndpoint,
        };

        await _mediator.Send(new UpdateBrandingCommand(tenantId, branding), cancellationToken);
        return NoContent();
    }

    [HttpGet("{tenantId:guid}/dashboard")]
    [ProducesResponseType(typeof(TenantDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(Guid tenantId, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetTenantDashboardQuery(tenantId), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{tenantId:guid}/invite-codes")]
    [ProducesResponseType(typeof(GenerateInviteCodeResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateInviteCode(
        Guid tenantId,
        [FromBody] GenerateInviteCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GenerateInviteCodeCommand(tenantId, request.StudentUserId),
            cancellationToken);

        return Created(
            $"/tenants/{tenantId}/invite-codes",
            new GenerateInviteCodeResponse
            {
                Code = result.Code,
                ExpiresAtUtc = result.ExpiresAtUtc,
            });
    }
}
