using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class TenantEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public TenantEndpointsTests(UnauthorizedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_dashboard_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Guid tenantId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/tenants/{tenantId}/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_branding_logo_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Guid tenantId = Guid.NewGuid();

        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1.0/tenants/{tenantId}/branding/logo",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
