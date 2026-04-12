using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Assessments.Queries.GetStudentAssessmentResult;

public sealed record GetStudentAssessmentResultQuery(Guid TenantId, Guid AssessmentId, Guid StudentId)
    : IRequest<SubmitAssessmentResultDto>, ITenantScopedRequest;
