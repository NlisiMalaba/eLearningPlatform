using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class AdaptiveEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public AdaptiveEndpointsTests(UnauthorizedApiFactory factory)
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
