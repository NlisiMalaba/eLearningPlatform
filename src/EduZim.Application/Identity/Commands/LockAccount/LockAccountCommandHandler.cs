using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace EduZim.Application.Identity.Commands.LockAccount;

public sealed class LockAccountCommandHandler : IRequestHandler<LockAccountCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public LockAccountCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Unit> Handle(LockAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);

        if (request.LockoutEnd is null)
        {
            await _userManager.SetLockoutEndDateAsync(user, null).ConfigureAwait(false);
            await _userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, request.LockoutEnd.Value).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
