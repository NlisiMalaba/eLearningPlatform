using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class GamificationEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public GamificationEndpointsTests(UnauthorizedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_points_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid studentId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/gamification/{studentId}/points");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_badges_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid studentId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/gamification/{studentId}/badges");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_leaderboard_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid tenantId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/gamification/leaderboard/{tenantId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
