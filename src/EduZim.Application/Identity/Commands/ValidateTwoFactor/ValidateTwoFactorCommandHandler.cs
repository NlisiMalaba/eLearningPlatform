using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace EduZim.Application.Identity.Commands.ValidateTwoFactor;

public sealed class ValidateTwoFactorCommandHandler : IRequestHandler<ValidateTwoFactorCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ValidateTwoFactorCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> Handle(ValidateTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);

        if (!user.TwoFactorEnabled)
            throw new ConflictException("Two-factor authentication is not enabled for this account.");

        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            request.Code).ConfigureAwait(false);

        return valid;
    }
}
