using MediatR;

namespace EduZim.Application.Identity.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(Guid UserId, string Token) : IRequest<Unit>;
