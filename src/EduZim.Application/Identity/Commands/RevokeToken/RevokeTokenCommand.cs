using MediatR;

namespace EduZim.Application.Identity.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest<bool>;
