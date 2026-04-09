using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Identity.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public RevokeTokenCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        return _refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
    }
}
