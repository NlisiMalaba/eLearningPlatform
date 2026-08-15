using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Assessments.Queries.ListSchoolClasses;

public sealed record SchoolClassListItemDto(Guid Id, string Name);

public sealed record ListSchoolClassesQuery(Guid TenantId)
    : IRequest<IReadOnlyList<SchoolClassListItemDto>>, ITenantScopedRequest;
