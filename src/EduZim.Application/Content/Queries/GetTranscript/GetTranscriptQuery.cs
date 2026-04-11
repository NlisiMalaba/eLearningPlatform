using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Content.Queries.GetTranscript;

public sealed record GetTranscriptQuery(Guid TenantId, Guid ContentId)
    : IRequest<TranscriptSignedUrlDto>, ITenantScopedRequest;
