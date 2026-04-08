using EduZim.API.Contracts;
using EduZim.Application.Common.Auth;
using EduZim.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.SignInWithPasswordAsync(
            request.Email,
            request.Password,
            cancellationToken);

        return result.Status switch
        {
            SignInWithPasswordStatus.Success => Ok(
                new LoginResponse
                {
                    AccessToken = result.AccessToken!,
                    ExpiresAt = result.ExpiresAt!.Value,
                }),
            SignInWithPasswordStatus.InvalidCredentials => Unauthorized(),
            SignInWithPasswordStatus.LockedOut => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                detail: "Account is locked out. Try again later or reset your password.",
                title: "Account locked"),
            SignInWithPasswordStatus.EmailNotConfirmed => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                detail: "Email address must be confirmed before signing in.",
                title: "Email not confirmed"),
            _ => throw new InvalidOperationException($"Unexpected sign-in status: {result.Status}."),
        };
    }
}
