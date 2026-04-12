using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Assessments.Commands.AutoSubmitAssessment;

public sealed record AutoSubmitAssessmentCommand(Guid TenantId, Guid AttemptId)
    : IRequest<Unit>, ITenantScopedRequest;
