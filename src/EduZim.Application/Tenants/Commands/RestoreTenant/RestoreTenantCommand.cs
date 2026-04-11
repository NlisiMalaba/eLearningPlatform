using MediatR;

namespace EduZim.Application.Tenants.Commands.RestoreTenant;

public sealed record RestoreTenantCommand(Guid TenantId) : IRequest;
