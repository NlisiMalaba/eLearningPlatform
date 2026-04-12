using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Assessments.Commands.BeginAssessmentSession;

public sealed record BeginAssessmentSessionCommand(
    Guid TenantId,
    Guid AssessmentId,
    Guid SchoolClassId) : IRequest<BeginAssessmentSessionResultDto>, ITenantScopedRequest;
