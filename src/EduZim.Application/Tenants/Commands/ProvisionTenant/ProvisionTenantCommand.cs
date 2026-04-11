using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Tenants.Commands.ProvisionTenant;

public sealed record ProvisionTenantCommand(string Name, TenantTier Tier, BrandingSettings? InitialBranding) : IRequest<Guid>;
