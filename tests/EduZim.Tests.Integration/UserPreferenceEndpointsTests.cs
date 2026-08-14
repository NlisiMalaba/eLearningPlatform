using System.Net;
using EduZim.API;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

public sealed class UserPreferenceEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UserPreferenceEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Put_font_size_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Guid userId = Guid.NewGuid();

        HttpResponseMessage response = await client.PutAsync(
            $"/api/v1.0/users/{userId}/font-size",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
