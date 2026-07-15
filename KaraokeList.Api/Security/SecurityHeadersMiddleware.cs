namespace KaraokeList.Security;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private static readonly string ApiVersion =
        typeof(SecurityHeadersMiddleware).Assembly.GetName().Version?.ToString() ?? "2.2.0";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        headers["X-Api-Version"] = ApiVersion;

        await next(context);
    }
}
