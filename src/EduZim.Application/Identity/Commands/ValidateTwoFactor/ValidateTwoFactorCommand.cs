using MediatR;

namespace EduZim.Application.Identity.Commands.ValidateTwoFactor;

public sealed record ValidateTwoFactorCommand(Guid UserId, string Code) : IRequest<bool>;
