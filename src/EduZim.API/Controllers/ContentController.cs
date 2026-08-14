using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.API.Routing;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Commands.ArchiveContent;
using EduZim.Application.Content.Commands.PublishContent;
using EduZim.Application.Content.Commands.UploadContent;
using EduZim.Application.Content.Queries.GetCaptions;
using EduZim.Application.Content.Queries.GetContentById;
using EduZim.Application.Content.Queries.GetTranscript;
using EduZim.Application.Content.Queries.ListContent;
using EduZim.Application.Tenants;
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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ContentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] Guid? tenantId, CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        IReadOnlyList<ContentListItemDto> items = await _mediator.Send(
                new ListContentQuery(TenantId(tenantId)),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(items);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadContentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadContentForm form,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantId(tenantId);
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, resolvedTenantId);
        if (form.File is not { Length: > 0 })
            return Problem(
                title: "File required",
                detail: "Upload a non-empty file.",
                statusCode: StatusCodes.Status400BadRequest);

        await using Stream stream = form.File.OpenReadStream();
        string contentType = string.IsNullOrWhiteSpace(form.File.ContentType)
            ? "application/octet-stream"
            : form.File.ContentType;
        Guid id = await _mediator.Send(
                new UploadContentCommand(
                    resolvedTenantId,
                    form.Title,
                    form.Type,
                    form.Language,
                    form.File.Length,
                    stream,
                    contentType),
                cancellationToken)
            .ConfigureAwait(false);
        return Created($"/{ApiRoutes.V1Content}/{id}", new UploadContentResponse { ContentId = id });
    }

    [HttpGet("{contentId:guid}")]
    [ProducesResponseType(typeof(ContentDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContent(
        Guid contentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        ContentDetailDto dto = await _mediator.Send(
                new GetContentByIdQuery(TenantId(tenantId), contentId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("{contentId:guid}/publish")]
    [ProducesResponseType(typeof(ContentDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(
        Guid contentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        ContentDetailDto dto = await _mediator.Send(
                new PublishContentCommand(TenantId(tenantId), contentId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpDelete("{contentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteContent(
        Guid contentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantId(tenantId);
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, resolvedTenantId);
        await _mediator.Send(new ArchiveContentCommand(resolvedTenantId, contentId), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("{contentId:guid}/captions")]
    [ProducesResponseType(typeof(IReadOnlyList<CaptionTrackSignedUrlDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaptions(
        Guid contentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        IReadOnlyList<CaptionTrackSignedUrlDto> items = await _mediator.Send(
                new GetCaptionsQuery(TenantId(tenantId), contentId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(items);
    }

    [HttpGet("{contentId:guid}/transcript")]
    [ProducesResponseType(typeof(TranscriptSignedUrlDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTranscript(
        Guid contentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantError(tenantId) is { } err)
            return err;

        TranscriptSignedUrlDto dto = await _mediator.Send(
                new GetTranscriptQuery(TenantId(tenantId), contentId),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    private IActionResult? TenantError(Guid? tenantId) =>
        TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId);

    private Guid TenantId(Guid? tenantId) =>
        TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
}
