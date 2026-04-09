using EduZim.Application.Common.Auth;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace EduZim.Application.Identity.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IEmailService _emailService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenService refreshTokenService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _accessTokenIssuer = accessTokenIssuer;
        _refreshTokenService = refreshTokenService;
        _emailService = emailService;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
            return new LoginFailed(SignInWithPasswordStatus.InvalidCredentials);

        var check = await _signInManager
            .CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)
            .ConfigureAwait(false);

        if (check.IsLockedOut)
        {
            await SendLockoutEmailAsync(user, cancellationToken).ConfigureAwait(false);
            return new LoginFailed(SignInWithPasswordStatus.LockedOut);
        }

        if (check.IsNotAllowed)
            return new LoginFailed(SignInWithPasswordStatus.EmailNotConfirmed);

        if (!check.Succeeded)
            return new LoginFailed(SignInWithPasswordStatus.InvalidCredentials);

        var (access, accessExp) = _accessTokenIssuer.IssueForUser(user);
        var (refresh, refreshExp) = await _refreshTokenService.IssueAsync(user.Id, cancellationToken).ConfigureAwait(false);

        return new LoginSucceeded(access, accessExp, refresh, refreshExp);
    }

    private async Task SendLockoutEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(email))
            return;

        var html =
            """
            <p>Your EduZim account was locked after multiple failed sign-in attempts.</p>
            <p>The lock will lift automatically after 15 minutes, or contact your administrator.</p>
            """;

        await _emailService.SendAsync(email, "Your EduZim account was locked", html, cancellationToken).ConfigureAwait(false);
    }
}
