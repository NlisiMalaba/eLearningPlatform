using Asp.Versioning;
using EduZim.API.Contracts;
using EduZim.API.Routing;
using EduZim.Application.Common.Auth;
using EduZim.Application.Identity.Commands.LockAccount;
using EduZim.Application.Identity.Commands.Login;
using EduZim.Application.Identity.Commands.RefreshToken;
using EduZim.Application.Identity.Commands.Register;
using EduZim.Application.Identity.Commands.RevokeToken;
using EduZim.Application.Identity.Commands.ValidateTwoFactor;
using EduZim.Application.Identity.Commands.VerifyEmail;
using EduZim.Application.Identity.Queries.SsoRedirect;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
[Tags("Authentication")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new RegisterCommand(
                request.Email,
                request.Password,
                request.FullName,
                request.PhoneNumber,
                request.Role,
                request.TenantId),
            cancellationToken);

        return Created($"/{ApiRoutes.V1Auth}/register/{id}", new RegisterResponse { UserId = id });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

        return result switch
        {
            LoginSucceeded s => Ok(
                new LoginResponse
                {
                    AccessToken = s.AccessToken,
                    AccessTokenExpiresAt = s.AccessTokenExpiresAt,
                    RefreshToken = s.RefreshToken,
                    RefreshTokenExpiresAt = s.RefreshTokenExpiresAt,
                }),
            LoginFailed f => MapLoginFailure(f),
            _ => throw new InvalidOperationException(),
        };
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var pair = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return Ok(
            new LoginResponse
            {
                AccessToken = pair.AccessToken,
                AccessTokenExpiresAt = pair.AccessTokenExpiresAt,
                RefreshToken = pair.RefreshToken,
                RefreshTokenExpiresAt = pair.RefreshTokenExpiresAt,
            });
    }

    [HttpPost("revoke")]
    [AllowAnonymous]
    public Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        return RevokeRefreshToken(request, cancellationToken);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public Task<IActionResult> Logout([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        return RevokeRefreshToken(request, cancellationToken);
    }

    private async Task<IActionResult> RevokeRefreshToken(RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var ok = await _mediator.Send(new RevokeTokenCommand(request.RefreshToken), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] Guid userId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        await _mediator.Send(new VerifyEmailCommand(userId, token), cancellationToken);
        return Ok();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public Task<IActionResult> VerifyEmailPost([FromBody] VerifyEmailBody body, CancellationToken cancellationToken)
    {
        return VerifyEmail(body.UserId, body.Token, cancellationToken);
    }

    [HttpPost("lock/{userId:guid}")]
    [Authorize(Roles = "PlatformAdmin,SchoolAdmin")]
    public async Task<IActionResult> LockAccount(
        Guid userId,
        [FromBody] LockAccountRequest? body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new LockAccountCommand(userId, body?.LockoutEnd), cancellationToken);
        return NoContent();
    }

    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateTwoFactor([FromBody] ValidateTwoFactorBody body, CancellationToken cancellationToken)
    {
        var ok = await _mediator.Send(
            new ValidateTwoFactorCommand(body.UserId, body.Code),
            cancellationToken);
        return Ok(ok);
    }

    [HttpGet("sso/{tenantId:guid}")]
    [HttpPost("sso/{tenantId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> SsoRedirect(Guid tenantId, CancellationToken cancellationToken)
    {
        var uri = await _mediator.Send(new SsoRedirectQuery(tenantId), cancellationToken);
        return Redirect(uri.ToString());
    }

    private IActionResult MapLoginFailure(LoginFailed f)
    {
        return f.Status switch
        {
            SignInWithPasswordStatus.InvalidCredentials => Unauthorized(),
            SignInWithPasswordStatus.LockedOut => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                detail: "Account is locked out. Try again later or reset your password.",
                title: "Account locked"),
            SignInWithPasswordStatus.EmailNotConfirmed => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                detail: "Email address must be confirmed before signing in.",
                title: "Email not confirmed"),
            _ => throw new InvalidOperationException(),
        };
    }
}
