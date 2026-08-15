using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class ContentEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public ContentEndpointsTests(UnauthorizedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_transcript_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Guid contentId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/content/{contentId}/transcript");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_content_list_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/api/v1.0/content");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_publish_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Guid contentId = Guid.NewGuid();

        HttpResponseMessage response = await client.PostAsync($"/api/v1.0/content/{contentId}/publish", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
