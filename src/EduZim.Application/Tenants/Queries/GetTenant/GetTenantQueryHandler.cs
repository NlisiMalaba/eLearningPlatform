using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants.Models;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Tenants.Queries.GetTenant;

public sealed class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantDetailsDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStorageService _storage;
    private readonly ContentStorageOptions _options;

    public GetTenantQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IStorageService storage,
        IOptions<ContentStorageOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _storage = storage;
        _options = options.Value;
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
                await ResolveLogoUrlAsync(b.LogoUrl).ConfigureAwait(false),
                b.SsoAuthorizationEndpoint));
    }

    private async Task<string?> ResolveLogoUrlAsync(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored) || BrandingLogoRules.IsHttpUrl(stored))
            return stored;

        TimeSpan expiry = TimeSpan.FromMinutes(_options.SignedUrlExpiryMinutes);
        return await _storage.GetSignedUrlAsync(stored, expiry).ConfigureAwait(false);
    }
}
