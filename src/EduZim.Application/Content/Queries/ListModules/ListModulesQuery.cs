using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Content.Queries.ListModules;

public sealed record ModuleListItemDto(
    Guid Id,
    string Title,
    GradeLevel Grade,
    string Subject,
    int SequenceOrder,
    bool IsRequired,
    int ContentItemCount);

public sealed record ListModulesQuery(Guid TenantId)
    : IRequest<IReadOnlyList<ModuleListItemDto>>, ITenantScopedRequest;
