using System.Net;
using EduZim.API;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

public sealed class TenantEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TenantEndpointsTests(WebApplicationFactory<Program> factory)
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
