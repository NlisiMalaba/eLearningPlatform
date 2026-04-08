using EduZim.Application.Common.Auth;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace EduZim.Infrastructure.Identity;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly JwtAccessTokenIssuer _jwtIssuer;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtAccessTokenIssuer jwtIssuer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtIssuer = jwtIssuer;
    }

    public async Task<SignInWithPasswordResult> SignInWithPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null)
        {
            return new SignInWithPasswordResult { Status = SignInWithPasswordStatus.InvalidCredentials };
        }

        var check = await _signInManager
            .CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)
            .ConfigureAwait(false);

        if (check.IsLockedOut)
        {
            return new SignInWithPasswordResult { Status = SignInWithPasswordStatus.LockedOut };
        }

        if (check.IsNotAllowed)
        {
            return new SignInWithPasswordResult { Status = SignInWithPasswordStatus.EmailNotConfirmed };
        }

        if (!check.Succeeded)
        {
            return new SignInWithPasswordResult { Status = SignInWithPasswordStatus.InvalidCredentials };
        }

        var (token, expires) = _jwtIssuer.IssueForUser(user);
        return new SignInWithPasswordResult
        {
            Status = SignInWithPasswordStatus.Success,
            AccessToken = token,
            ExpiresAt = expires,
        };
    }
}
