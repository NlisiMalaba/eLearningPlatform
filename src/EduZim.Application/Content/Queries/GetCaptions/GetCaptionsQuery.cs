using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Content.Queries.GetCaptions;

public sealed record GetCaptionsQuery(Guid TenantId, Guid ContentId)
    : IRequest<IReadOnlyList<CaptionTrackSignedUrlDto>>, ITenantScopedRequest;
