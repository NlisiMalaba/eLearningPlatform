using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.API.Routing;
using EduZim.Application.Content.Commands.ArchiveContent;
using EduZim.Application.Content.Commands.UploadContent;
using EduZim.Application.Content.Queries.GetCaptions;
using EduZim.Application.Content.Queries.GetContentById;
using EduZim.Application.Content.Queries.GetTranscript;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/content")]
[Tags("Content")]
[Authorize]
public sealed class ContentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ContentController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadContentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadContentForm form,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (ValidateTenantQuery(tenantId) is { } err)
            return err;

        var resolvedTenantId = ResolveTenantId(tenantId);
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, resolvedTenantId);

        if (form.File is not { Length: > 0 })
            return Problem(
                title: "File required",
                detail: "Upload a non-empty file.",
                statusCode: StatusCodes.Status400BadRequest);

        await using var stream = form.File.OpenReadStream();
        var contentType = string.IsNullOrWhiteSpace(form.File.ContentType)
            ? "application/octet-stream"
            : form.File.ContentType;

        var id = await _mediator.Send(
            new UploadContentCommand(
                resolvedTenantId,
                form.Title,
                form.Type,
                form.Language,
                form.File.Length,
                stream,
                contentType),
            cancellationToken);

        return Created(
            $"/{ApiRoutes.V1Content}/{id}",
            new UploadContentResponse { ContentId = id });
    }

    [HttpGet("{contentId:guid}")]
    [ProducesResponseType(typeof(ContentDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContent(
        Guid contentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (ValidateTenantQuery(tenantId) is { } err)
            return err;

        var resolvedTenantId = ResolveTenantId(tenantId);
        TenantAccessHelper.EnsureCanAccessTenantScope(_currentUser, resolvedTenantId);

        var dto = await _mediator.Send(new GetContentByIdQuery(resolvedTenantId, contentId), cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{contentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteContent(
        Guid contentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (ValidateTenantQuery(tenantId) is { } err)
            return err;

        var resolvedTenantId = ResolveTenantId(tenantId);
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, resolvedTenantId);

        await _mediator.Send(new ArchiveContentCommand(resolvedTenantId, contentId), cancellationToken);
        return NoContent();
    }

    [HttpGet("{contentId:guid}/captions")]
    [ProducesResponseType(typeof(IReadOnlyList<CaptionTrackSignedUrlDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaptions(
        Guid contentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (ValidateTenantQuery(tenantId) is { } err)
            return err;

        var resolvedTenantId = ResolveTenantId(tenantId);
        TenantAccessHelper.EnsureCanAccessTenantScope(_currentUser, resolvedTenantId);

        var items = await _mediator.Send(new GetCaptionsQuery(resolvedTenantId, contentId), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{contentId:guid}/transcript")]
    [ProducesResponseType(typeof(TranscriptSignedUrlDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTranscript(
        Guid contentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (ValidateTenantQuery(tenantId) is { } err)
            return err;

        var resolvedTenantId = ResolveTenantId(tenantId);
        TenantAccessHelper.EnsureCanAccessTenantScope(_currentUser, resolvedTenantId);

        TranscriptSignedUrlDto dto = await _mediator.Send(
            new GetTranscriptQuery(resolvedTenantId, contentId),
            cancellationToken);
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
