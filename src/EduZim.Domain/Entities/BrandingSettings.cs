namespace EduZim.Domain.Entities;

public class BrandingSettings
{
    public string? LogoUrl { get; set; }
    public string SchoolName { get; set; } = default!;
    public string PrimaryColour { get; set; } = "#1976D2";

    /// <summary>Optional tenant IdP / OIDC authorization endpoint for browser SSO redirect.</summary>
    public string? SsoAuthorizationEndpoint { get; set; }
}
