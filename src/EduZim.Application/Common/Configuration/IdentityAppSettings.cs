namespace EduZim.Application.Common.Configuration;

public sealed class IdentityAppSettings
{
    public const string SectionName = "Identity";

    /// <summary>Public base URL of the API (scheme + host, no trailing slash) used to build email links.</summary>
    public string PublicAppBaseUrl { get; set; } = "https://localhost";

    /// <summary>Relative path appended to <see cref="PublicAppBaseUrl"/> for email confirmation links (query params added by the app).</summary>
    public string EmailConfirmationRelativePath { get; set; } = "/api/auth/verify-email";
}
