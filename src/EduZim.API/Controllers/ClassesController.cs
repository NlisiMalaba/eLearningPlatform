using Asp.Versioning;
using EduZim.Application.Assessments.Queries.ListSchoolClasses;
using EduZim.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/classes")]
[Tags("Classes")]
[Authorize]
public sealed class ClassesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ClassesController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SchoolClassListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] Guid? tenantId, CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        IReadOnlyList<SchoolClassListItemDto> items = await _mediator.Send(
                new ListSchoolClassesQuery(TenantQueryResolution.ResolveTenantId(_currentUser, tenantId)),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(items);
    }
}
