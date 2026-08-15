using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class ZimBotEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public ZimBotEndpointsTests(UnauthorizedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_chat_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsync("/api/v1.0/zimbot/chat", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_logs_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid tenantId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/zimbot/logs/{tenantId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
