using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class SyncEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public SyncEndpointsTests(UnauthorizedApiFactory factory)
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
