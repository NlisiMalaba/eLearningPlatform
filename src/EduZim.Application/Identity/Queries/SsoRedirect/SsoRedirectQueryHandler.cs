using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;

namespace EduZim.Application.Identity.Queries.SsoRedirect;

public sealed class SsoRedirectQueryHandler : IRequestHandler<SsoRedirectQuery, Uri>
{
    private readonly IRepository<Tenant> _tenants;

    public SsoRedirectQueryHandler(IRepository<Tenant> tenants)
    {
        _tenants = tenants;
    }

    public async Task<Uri> Handle(SsoRedirectQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenants.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        var endpoint = tenant.Branding.SsoAuthorizationEndpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new NotFoundException("SsoAuthorizationEndpoint", request.TenantId);

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "SsoAuthorizationEndpoint", new[] { "Tenant SSO endpoint must be an absolute URL." } },
            });

        return uri;
    }
}
