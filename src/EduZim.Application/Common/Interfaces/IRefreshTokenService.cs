using EduZim.Domain.Entities;

namespace EduZim.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    Task<(string RawToken, DateTimeOffset ExpiresAt)> IssueAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Consumes the refresh token (revokes it) and returns the user if it was valid.</summary>
    Task<ApplicationUser?> RotateAsync(string rawRefreshToken, CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(string rawRefreshToken, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
