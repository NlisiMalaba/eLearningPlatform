using MediatR;

namespace EduZim.Application.Identity.Commands.LockAccount;

/// <param name="LockoutEnd">When null, the account is unlocked; otherwise lock until this instant (UTC).</param>
public sealed record LockAccountCommand(Guid UserId, DateTimeOffset? LockoutEnd) : IRequest<Unit>;
