using EduZim.Application.Exceptions;
using EduZim.Domain.Exceptions;

namespace EduZim.Application.Tenants;

public static class BrandingLogoRules
{
    public const long MaxBytes = 2L * 1024 * 1024;

    public static bool IsAllowedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase);
    }

    public static void EnsureAllowed(string contentType, long fileSizeBytes)
    {
        if (!IsAllowedContentType(contentType))
            throw new DomainException("Logo must be a PNG, JPEG, or WebP image.");
        if (fileSizeBytes <= 0)
            throw new DomainException("Logo must be a non-empty file.");
        if (fileSizeBytes > MaxBytes)
            throw new PayloadTooLargeException("Logo must be at most 2 MB.");
    }

    public static bool IsHttpUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
}
