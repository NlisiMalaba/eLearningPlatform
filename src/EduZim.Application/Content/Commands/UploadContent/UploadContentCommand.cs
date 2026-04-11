using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Content.Commands.UploadContent;

public sealed record UploadContentCommand(
    Guid TenantId,
    string Title,
    ContentType Type,
    string Language,
    long FileSizeBytes,
    Stream Content,
    string ContentTypeHeader) : IRequest<Guid>, ITenantScopedRequest;
