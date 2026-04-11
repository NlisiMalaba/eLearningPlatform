using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Content.Commands.ArchiveContent;

public sealed record ArchiveContentCommand(Guid TenantId, Guid ContentId) : IRequest<Unit>, ITenantScopedRequest;
