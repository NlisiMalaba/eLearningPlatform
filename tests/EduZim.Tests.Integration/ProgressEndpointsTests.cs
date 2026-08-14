using System.Net;
using EduZim.API;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

public sealed class ProgressEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProgressEndpointsTests(WebApplicationFactory<Program> factory)
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

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
