namespace Pathway.Api.Hosting;

public static class SecurityHeaders
{
    /// <summary>Adds conservative security headers to every response. The API serves JSON only, so the CSP allows nothing.</summary>
    public static IApplicationBuilder UsePathwaySecurityHeaders(this WebApplication app)
    {
        var isProduction = app.Environment.IsProduction();
        return app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
            if (isProduction) context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            await next(context);
        });
    }
}
