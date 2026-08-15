using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Queries.GetContentById;
using MediatR;

namespace EduZim.Application.Content.Commands.PublishContent;

public sealed record PublishContentCommand(Guid TenantId, Guid ContentId)
    : IRequest<ContentDetailDto>, ITenantScopedRequest;
