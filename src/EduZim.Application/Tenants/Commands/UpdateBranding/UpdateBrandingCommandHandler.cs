using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Tenants.Commands.UpdateBranding;

public sealed class UpdateBrandingCommandHandler : IRequestHandler<UpdateBrandingCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateBrandingCommandHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateBrandingCommand request, CancellationToken cancellationToken)
    {
        TenantAccessHelper.EnsureCanManageTenantSettings(_currentUser, request.TenantId);

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        tenant.Branding.SchoolName = request.Branding.SchoolName;
        tenant.Branding.PrimaryColour = request.Branding.PrimaryColour;
        if (!string.IsNullOrWhiteSpace(request.Branding.LogoUrl)
            && BrandingLogoRules.IsHttpUrl(request.Branding.LogoUrl))
        {
            tenant.Branding.LogoUrl = request.Branding.LogoUrl;
        }

        tenant.Branding.SsoAuthorizationEndpoint = request.Branding.SsoAuthorizationEndpoint;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
