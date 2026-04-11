namespace EduZim.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer");

        // Scalar, OpenAPI JSON, and Hangfire are browser UIs that require scripts/styles.
        // A tight default-src 'none' CSP would render them as a blank page.
        if (!IsDevToolingPath(context.Request.Path))
        {
            context.Response.Headers.Append(
                "Content-Security-Policy",
                "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");
        }

        await _next(context);
    }

    private static bool IsDevToolingPath(PathString path)
    {
        var p = path.Value ?? string.Empty;
        return p.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase);
    }
}
