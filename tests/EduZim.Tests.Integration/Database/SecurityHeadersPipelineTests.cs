using Microsoft.AspNetCore.Mvc.Testing;

namespace EduZim.Tests.Integration.Database;

[Collection(PostgresRlsCollection.Name)]
public sealed class SecurityHeadersPipelineTests
{
    private readonly PostgresRlsFixture _fixture;

    public SecurityHeadersPipelineTests(PostgresRlsFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Health_response_includes_required_security_headers()
    {
        _fixture.EnsureDockerAvailable();

        using HttpClient client = _fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/api/v1.0/health");

        AssertHeader(response, "X-Content-Type-Options", "nosniff");
        AssertHeader(response, "X-Frame-Options", "DENY");
        string csp = ReadHeader(response, "Content-Security-Policy");
        Assert.Contains("default-src 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
    }

    private static void AssertHeader(HttpResponseMessage response, string name, string expected) =>
        Assert.Equal(expected, ReadHeader(response, name));

    private static string ReadHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out IEnumerable<string>? values))
            return values.Single();
        if (response.Content.Headers.TryGetValues(name, out IEnumerable<string>? contentValues))
            return contentValues.Single();

        Assert.Fail($"Missing response header '{name}'.");
        return string.Empty;
    }
}
