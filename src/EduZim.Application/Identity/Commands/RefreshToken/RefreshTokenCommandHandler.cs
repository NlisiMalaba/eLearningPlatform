using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using MediatR;

namespace EduZim.Application.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenPairResult>
{
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IRefreshTokenService _refreshTokenService;

    public RefreshTokenCommandHandler(
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenService refreshTokenService)
    {
        _accessTokenIssuer = accessTokenIssuer;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<TokenPairResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _refreshTokenService
            .RotateAsync(request.RefreshToken, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var (access, accessExp) = _accessTokenIssuer.IssueForUser(user);
        var (refresh, refreshExp) = await _refreshTokenService.IssueAsync(user.Id, cancellationToken).ConfigureAwait(false);

        return new TokenPairResult(access, accessExp, refresh, refreshExp);
    }
}
