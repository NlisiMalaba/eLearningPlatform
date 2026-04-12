using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Assessments.Queries.GetClassResults;

public sealed record GetClassResultsQuery(Guid TenantId, Guid AssessmentId, Guid SchoolClassId)
    : IRequest<ClassAssessmentResultsDto>, ITenantScopedRequest;
