using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class UserPreferenceEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public UserPreferenceEndpointsTests(UnauthorizedApiFactory factory)
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
