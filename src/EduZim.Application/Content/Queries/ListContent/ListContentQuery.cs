using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Content.Queries.ListContent;

public sealed record ContentListItemDto(
    Guid Id,
    string Title,
    ContentType Type,
    ContentStatus Status,
    long FileSizeBytes,
    DateTime CreatedAtUtc);

public sealed record ListContentQuery(Guid TenantId)
    : IRequest<IReadOnlyList<ContentListItemDto>>, ITenantScopedRequest;
