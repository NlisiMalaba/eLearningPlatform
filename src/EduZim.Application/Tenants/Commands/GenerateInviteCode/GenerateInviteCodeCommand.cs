using MediatR;

namespace EduZim.Application.Tenants.Commands.GenerateInviteCode;

public sealed record GenerateInviteCodeCommand(Guid TenantId, Guid StudentUserId) : IRequest<GeneratedInviteCodeDto>;

public sealed record GeneratedInviteCodeDto(string Code, DateTime ExpiresAtUtc);
