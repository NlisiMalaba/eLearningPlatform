using MediatR;

namespace EduZim.Application.Tenants.Commands.RedeemInviteCode;

/// <summary>Links the current parent user to the student associated with a valid tenant invite code.</summary>
public sealed record RedeemInviteCodeCommand(string Code) : IRequest;
