using EduZim.Application.Common.Auth;
using MediatR;

namespace EduZim.Application.Identity.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public abstract record LoginResult;

public sealed record LoginSucceeded(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt) : LoginResult;

public sealed record LoginFailed(SignInWithPasswordStatus Status) : LoginResult;
