namespace GiftOfTheGivers.Infrastructure;

/// <summary>Adds defence-in-depth response headers to every response, including static files.</summary>
public static class SecurityHeaders
{
    // Inline scripts/styles are still used by several views, so 'unsafe-inline' is allowed for
    // now; the policy still restricts every resource to known origins and blocks framing.
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self'; " +
        "form-action 'self' https://sandbox.payfast.co.za https://www.payfast.co.za; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "object-src 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            await next();
        });
}
