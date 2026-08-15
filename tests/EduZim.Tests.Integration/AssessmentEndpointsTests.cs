using System.Net;
using EduZim.API;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration;

[Collection(UnauthorizedApiCollection.Name)]
public sealed class AssessmentEndpointsTests
{
    private readonly UnauthorizedApiFactory _factory;

    public AssessmentEndpointsTests(UnauthorizedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_assessments_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/api/v1.0/assessments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_class_results_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Guid assessmentId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1.0/assessments/{assessmentId}/class-results?schoolClassId={classId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_classes_without_auth_returns_unauthorized()
    {
        HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/api/v1.0/classes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
