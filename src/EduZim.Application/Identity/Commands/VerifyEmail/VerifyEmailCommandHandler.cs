using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace EduZim.Application.Identity.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public VerifyEmailCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Unit> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);

        var result = await _userManager.ConfirmEmailAsync(user, request.Token).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "_", result.Errors.Select(e => $"{e.Code}: {e.Description}").ToArray() },
            });
        }

        return Unit.Value;
    }
}
