using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduZim.Application.Common.Auth;
using EduZim.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using EduZim.Application.Common.Interfaces;

namespace EduZim.Infrastructure.Identity;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtSettings _settings;

    public JwtAccessTokenIssuer(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public (string Token, DateTimeOffset ExpiresAt) IssueForUser(ApplicationUser user)
    {
        var claims = BuildClaims(user);
        var expires = DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, expires);
    }

    private static IEnumerable<Claim> BuildClaims(ApplicationUser user)
    {
        var list = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        if (user.TenantId is { } tenantId)
            list.Add(new Claim(EduZimClaimTypes.TenantId, tenantId.ToString()));

        return list;
    }
}
