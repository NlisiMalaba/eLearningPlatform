using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants.Models;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Tenants.Queries.GetTenant;

public sealed class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantDetailsDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetTenantQueryHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TenantDetailsDto> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        TenantAccessHelper.EnsureCanViewTenantDashboard(_currentUser, request.TenantId);

        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        var b = tenant.Branding;
        return new TenantDetailsDto(
            tenant.Id,
            tenant.Name,
            tenant.Tier,
            tenant.Status,
            tenant.CreatedAt,
            tenant.SuspendedAtUtc,
            new TenantBrandingDto(
                b.SchoolName,
                b.PrimaryColour,
                b.LogoUrl,
                b.SsoAuthorizationEndpoint));
    }
}
