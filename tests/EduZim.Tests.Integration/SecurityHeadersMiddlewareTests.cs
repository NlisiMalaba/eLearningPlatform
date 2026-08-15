using EduZim.API.Middleware;
using Microsoft.AspNetCore.Http;

namespace EduZim.Tests.Integration;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Sets_content_type_frame_and_csp_headers()
    {
        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1.0/health";
        SecurityHeadersMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers.XContentTypeOptions);
        Assert.Equal("DENY", context.Response.Headers.XFrameOptions);
        string csp = context.Response.Headers.ContentSecurityPolicy.ToString();
        Assert.Contains("default-src 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Omits_strict_csp_on_dev_tooling_paths()
    {
        DefaultHttpContext context = new();
        context.Request.Path = "/scalar";
        SecurityHeadersMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers.XContentTypeOptions);
        Assert.Equal("DENY", context.Response.Headers.XFrameOptions);
        Assert.True(string.IsNullOrEmpty(context.Response.Headers.ContentSecurityPolicy));
    }
}
