using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class NotificationEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public NotificationEndpointsTests(UnauthorizedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_notifications_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid userId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/notifications/{userId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_notification_read_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid notificationId = Guid.NewGuid();

        HttpResponseMessage response = await client.PutAsync(
            $"/api/v1.0/notifications/{notificationId}/read",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_notification_preferences_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid userId = Guid.NewGuid();

        HttpResponseMessage response = await client.PutAsync(
            $"/api/v1.0/users/{userId}/notification-preferences",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
