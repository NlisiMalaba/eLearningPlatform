using System.Net;
using EduZim.API;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

public sealed class SyncEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SyncEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_sync_upload_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsync("/api/v1.0/sync/upload", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
