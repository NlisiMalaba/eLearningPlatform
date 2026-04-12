using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Assessments.Commands.AssignAssessment;

public sealed record AssignAssessmentCommand(
    Guid TenantId,
    Guid AssessmentId,
    Guid SchoolClassId,
    DateTime DueAtUtc) : IRequest<Unit>, ITenantScopedRequest;
