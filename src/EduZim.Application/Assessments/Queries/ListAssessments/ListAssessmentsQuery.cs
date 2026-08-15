using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Assessments.Queries.ListAssessments;

public sealed record AssessmentListItemDto(
    Guid Id,
    string Title,
    Guid ModuleId,
    int? TimeLimitSeconds,
    int PassingScorePercent,
    int QuestionCount);

public sealed record ListAssessmentsQuery(Guid TenantId)
    : IRequest<IReadOnlyList<AssessmentListItemDto>>, ITenantScopedRequest;
