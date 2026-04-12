using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.API.Routing;
using EduZim.Application.Assessments.Commands.AssignAssessment;
using EduZim.Application.Assessments.Commands.CreateAssessment;
using EduZim.Application.Assessments.Commands.SubmitAssessment;
using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Assessments.Queries.GetClassResults;
using EduZim.Application.Assessments.Queries.GetStudentAssessmentResult;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/assessments")]
[Tags("Assessments")]
[Authorize]
public sealed class AssessmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AssessmentsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateAssessmentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAssessment(
        [FromBody] CreateAssessmentRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, resolvedTenantId);

        IReadOnlyList<CreateAssessmentQuestionItem> questions = request.Questions
            .Select(
                q => new CreateAssessmentQuestionItem(
                    q.Type,
                    q.Text,
                    q.Points,
                    q.OptionTexts,
                    q.CorrectOptionIndex,
                    q.CorrectShortAnswer))
            .ToList();

        Guid id = await _mediator.Send(
                new CreateAssessmentCommand(
                    resolvedTenantId,
                    request.ModuleId,
                    request.Title,
                    request.TimeLimitSeconds,
                    request.PassingScorePercent,
                    questions),
                cancellationToken)
            .ConfigureAwait(false);

        return Created(
            $"/{ApiRoutes.V1Assessments}/{id}",
            new CreateAssessmentResponse { AssessmentId = id });
    }

    [HttpPost("{assessmentId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignAssessment(
        Guid assessmentId,
        [FromBody] AssignAssessmentRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, resolvedTenantId);

        await _mediator
            .Send(
                new AssignAssessmentCommand(
                    resolvedTenantId,
                    assessmentId,
                    request.SchoolClassId,
                    request.DueAtUtc),
                cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }

    [HttpPost("{assessmentId:guid}/submit")]
    [ProducesResponseType(typeof(SubmitAssessmentResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitAssessment(
        Guid assessmentId,
        [FromBody] SubmitAssessmentRequest request,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);
        TenantAccessHelper.EnsureCanAccessTenantScope(_currentUser, resolvedTenantId);

        IReadOnlyList<SubmitAssessmentAnswerItem> answers = request.Answers
            .Select(a => new SubmitAssessmentAnswerItem(a.QuestionId, a.Answer))
            .ToList();

        SubmitAssessmentResultDto result = await _mediator
            .Send(
                new SubmitAssessmentCommand(resolvedTenantId, assessmentId, request.AttemptId, answers),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    [HttpGet("{assessmentId:guid}/results/{studentId:guid}")]
    [ProducesResponseType(typeof(SubmitAssessmentResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentResults(
        Guid assessmentId,
        Guid studentId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);

        SubmitAssessmentResultDto dto = await _mediator.Send(
                new GetStudentAssessmentResultQuery(resolvedTenantId, assessmentId, studentId),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(dto);
    }

    [HttpGet("{assessmentId:guid}/class-results")]
    [ProducesResponseType(typeof(ClassAssessmentResultsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClassResults(
        Guid assessmentId,
        [FromQuery] Guid schoolClassId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (schoolClassId == Guid.Empty)
        {
            return Problem(
                title: "Class required",
                detail: "Provide schoolClassId (query).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (TenantQueryResolution.ValidateTenantQuery(this, _currentUser, tenantId) is { } err)
            return err;

        Guid resolvedTenantId = TenantQueryResolution.ResolveTenantId(_currentUser, tenantId);

        ClassAssessmentResultsDto dto = await _mediator.Send(
                new GetClassResultsQuery(resolvedTenantId, assessmentId, schoolClassId),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(dto);
    }
}
