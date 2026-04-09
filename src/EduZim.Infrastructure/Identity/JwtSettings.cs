namespace EduZim.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "EduZim";
    public string Audience { get; set; } = "EduZim.Api";
    public string SigningKey { get; set; } = default!;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
