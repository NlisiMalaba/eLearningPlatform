using MediatR;

namespace EduZim.Application.Tenants.Commands.UploadBrandingLogo;

public sealed record UploadBrandingLogoCommand(
    Guid TenantId,
    long FileSizeBytes,
    string ContentType,
    Stream Content) : IRequest<string>;
