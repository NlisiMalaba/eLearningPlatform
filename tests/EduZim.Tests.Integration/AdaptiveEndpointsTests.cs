using System.Net;
using EduZim.API;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

public sealed class AdaptiveEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdaptiveEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_weekly_summary_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        Guid studentId = Guid.NewGuid();
        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1.0/adaptive/{studentId}/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_path_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1.0/adaptive/{studentId}/path?moduleId={moduleId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_update_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        Guid studentId = Guid.NewGuid();
        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1.0/adaptive/{studentId}/update",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
