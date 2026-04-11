using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Content.Queries.GetSignedUrl;

public sealed record GetSignedUrlQuery(Guid TenantId, Guid ContentId, TimeSpan? UrlTtl = null)
    : IRequest<string>, ITenantScopedRequest;
