using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class ProgressEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public ProgressEndpointsTests(UnauthorizedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_student_progress_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid studentId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/students/{studentId}/progress");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_parent_dashboard_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid parentId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/parents/{parentId}/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_screen_time_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid studentId = Guid.NewGuid();

        HttpResponseMessage response = await client.PutAsync(
            $"/api/v1.0/students/{studentId}/screen-time",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
