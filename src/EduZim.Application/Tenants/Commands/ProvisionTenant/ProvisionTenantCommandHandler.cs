using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Tenants.Commands.ProvisionTenant;

public sealed class ProvisionTenantCommandHandler : IRequestHandler<ProvisionTenantCommand, Guid>
{
    private readonly IEduZimDbContext _db;

    public ProvisionTenantCommandHandler(IEduZimDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(ProvisionTenantCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var branding = request.InitialBranding ?? new BrandingSettings { SchoolName = request.Name, PrimaryColour = "#1976D2" };
        if (string.IsNullOrWhiteSpace(branding.SchoolName))
            branding.SchoolName = request.Name;

        var tenant = new Tenant
        {
            Id = id,
            Name = request.Name,
            Tier = request.Tier,
            Status = TenantStatus.Provisioning,
            Branding = branding,
            CreatedAt = DateTime.UtcNow,
        };

        await _db.Tenants.AddAsync(tenant, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        tenant.Status = TenantStatus.Active;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return id;
    }
}
