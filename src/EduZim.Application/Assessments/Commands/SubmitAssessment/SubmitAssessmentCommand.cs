using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Assessments.Commands.SubmitAssessment;

public sealed record SubmitAssessmentAnswerItem(Guid QuestionId, string? Answer);

public sealed record SubmitAssessmentCommand(
    Guid TenantId,
    Guid AttemptId,
    IReadOnlyList<SubmitAssessmentAnswerItem> Answers) : IRequest<SubmitAssessmentResultDto>, ITenantScopedRequest;
