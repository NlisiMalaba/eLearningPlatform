using EduZim.Domain.Entities;
using MediatR;

namespace EduZim.Application.Tenants.Commands.UpdateBranding;

public sealed record UpdateBrandingCommand(Guid TenantId, BrandingSettings Branding) : IRequest;
