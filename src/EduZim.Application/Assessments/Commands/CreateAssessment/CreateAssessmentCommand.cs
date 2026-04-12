using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Assessments.Commands.CreateAssessment;

public sealed record CreateAssessmentQuestionItem(
    QuestionType Type,
    string Text,
    int Points,
    IReadOnlyList<string>? OptionTexts,
    int? CorrectOptionIndex,
    string? CorrectShortAnswer);

public sealed record CreateAssessmentCommand(
    Guid TenantId,
    Guid ModuleId,
    string Title,
    int? TimeLimitSeconds,
    int PassingScorePercent,
    IReadOnlyList<CreateAssessmentQuestionItem> Questions) : IRequest<Guid>, ITenantScopedRequest;
