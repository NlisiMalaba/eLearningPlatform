using System.Net;
using EduZim.API;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

public sealed class ClassroomEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ClassroomEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_classrooms_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsync("/api/v1.0/classrooms", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_join_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid sessionId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/classrooms/{sessionId}/join");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_end_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid sessionId = Guid.NewGuid();

        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1.0/classrooms/{sessionId}/end",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_attendance_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid sessionId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/classrooms/{sessionId}/attendance");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_recording_without_auth_returns_unauthorized()
    {
        HttpClient client = CreateClient();
        Guid sessionId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1.0/classrooms/{sessionId}/recording");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
