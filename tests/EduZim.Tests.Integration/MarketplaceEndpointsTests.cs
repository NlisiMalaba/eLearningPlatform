using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class MarketplaceEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public MarketplaceEndpointsTests(UnauthorizedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_packs_without_auth_returns_unauthorized()
    {
        HttpResponseMessage response = await CreateClient().PostAsync("/api/v1.0/marketplace/packs", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_packs_without_auth_returns_unauthorized()
    {
        HttpResponseMessage response = await CreateClient().GetAsync("/api/v1.0/marketplace/packs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_pack_by_id_without_auth_returns_unauthorized()
    {
        Guid packId = Guid.NewGuid();
        HttpResponseMessage response = await CreateClient().GetAsync($"/api/v1.0/marketplace/packs/{packId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_approve_without_auth_returns_unauthorized()
    {
        Guid packId = Guid.NewGuid();
        HttpResponseMessage response = await CreateClient()
            .PostAsync($"/api/v1.0/marketplace/packs/{packId}/approve", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_request_access_without_auth_returns_unauthorized()
    {
        Guid packId = Guid.NewGuid();
        HttpResponseMessage response = await CreateClient()
            .PostAsync($"/api/v1.0/marketplace/packs/{packId}/request-access", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_approve_access_without_auth_returns_unauthorized()
    {
        Guid packId = Guid.NewGuid();
        HttpResponseMessage response = await CreateClient()
            .PostAsync($"/api/v1.0/marketplace/packs/{packId}/approve-access", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_rate_without_auth_returns_unauthorized()
    {
        Guid packId = Guid.NewGuid();
        HttpResponseMessage response = await CreateClient()
            .PostAsync($"/api/v1.0/marketplace/packs/{packId}/rate", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_pack_without_auth_returns_unauthorized()
    {
        Guid packId = Guid.NewGuid();
        HttpResponseMessage response = await CreateClient()
            .DeleteAsync($"/api/v1.0/marketplace/packs/{packId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
