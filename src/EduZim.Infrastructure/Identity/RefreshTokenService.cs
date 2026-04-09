using System.Security.Cryptography;
using System.Text;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduZim.Infrastructure.Identity;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly EduZimDbContext _db;
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;

    public RefreshTokenService(
        EduZimDbContext db,
        IOptions<JwtSettings> jwtOptions,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _jwtSettings = jwtOptions.Value;
        _userManager = userManager;
    }

    public async Task<(string RawToken, DateTimeOffset ExpiresAt)> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var raw = CreateRawToken();
        var hash = HashRawToken(raw);
        var expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = expires,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (raw, expires);
    }

    public async Task<ApplicationUser?> RotateAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var entry = await FindActiveTokenEntryAsync(rawRefreshToken, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return null;

        entry.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await _userManager.FindByIdAsync(entry.UserId.ToString()).ConfigureAwait(false);
    }

    public async Task<bool> RevokeAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var entry = await FindActiveTokenEntryAsync(rawRefreshToken, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return false;

        entry.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<RefreshToken?> FindActiveTokenEntryAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        var hash = HashRawToken(rawRefreshToken);
        var now = DateTimeOffset.UtcNow;
        return await _db.RefreshTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash && t.RevokedAt == null && t.ExpiresAt > now,
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal static string HashRawToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    private static string CreateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
