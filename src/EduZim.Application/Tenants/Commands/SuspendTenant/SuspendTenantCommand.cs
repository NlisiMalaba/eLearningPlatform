using MediatR;

namespace EduZim.Application.Tenants.Commands.SuspendTenant;

public sealed record SuspendTenantCommand(Guid TenantId) : IRequest;
