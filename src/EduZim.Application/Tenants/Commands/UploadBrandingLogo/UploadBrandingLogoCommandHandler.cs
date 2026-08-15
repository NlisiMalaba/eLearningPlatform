using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Tenants.Commands.UploadBrandingLogo;

public sealed class UploadBrandingLogoCommandHandler : IRequestHandler<UploadBrandingLogoCommand, string>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStorageService _storage;
    private readonly ContentStorageOptions _options;
    private readonly ILogger<UploadBrandingLogoCommandHandler> _logger;

    public UploadBrandingLogoCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IStorageService storage,
        IOptions<ContentStorageOptions> options,
        ILogger<UploadBrandingLogoCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> Handle(UploadBrandingLogoCommand request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanManageTenantSettings(_currentUser, request.TenantId);
        BrandingLogoRules.EnsureAllowed(request.ContentType, request.FileSizeBytes);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        Tenant tenant = await LoadTenantAsync(request.TenantId, ct).ConfigureAwait(false);
        string key = $"{request.TenantId:N}/branding/logo";
        await _storage.UploadAsync(key, request.Content, request.ContentType, ct).ConfigureAwait(false);
        tenant.Branding.LogoUrl = key;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Updated branding logo for tenant {TenantId}.", request.TenantId);
        TimeSpan expiry = TimeSpan.FromMinutes(_options.SignedUrlExpiryMinutes);
        return await _storage.GetSignedUrlAsync(key, expiry).ConfigureAwait(false);
    }

    private async Task<Tenant> LoadTenantAsync(Guid tenantId, CancellationToken ct)
    {
        Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct).ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), tenantId);

        return tenant;
    }
}
