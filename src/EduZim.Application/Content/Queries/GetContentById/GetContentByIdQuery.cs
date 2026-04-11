using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Content.Queries.GetContentById;

public sealed record GetContentByIdQuery(Guid TenantId, Guid ContentId)
    : IRequest<ContentDetailDto>, ITenantScopedRequest;
