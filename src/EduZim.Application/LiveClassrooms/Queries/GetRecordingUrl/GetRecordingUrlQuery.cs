using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.LiveClassrooms.Queries.GetRecordingUrl;

public sealed record GetRecordingUrlQuery(Guid TenantId, Guid SessionId)
    : IRequest<string?>, ITenantScopedRequest;
