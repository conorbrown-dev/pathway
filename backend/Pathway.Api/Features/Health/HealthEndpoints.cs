using Pathway.Api.Hosting;

namespace Pathway.Api.Features.Health;

/// <summary>Liveness (/health), readiness (/ready: the database is reachable) and a minimal OpenAPI stub.</summary>
public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok", courseVersion = CourseCatalog.Version })).RequireRateLimiting(RateLimitPolicies.PublicRead);
        app.MapGet("/ready", Ready).RequireRateLimiting(RateLimitPolicies.PublicRead);
        app.MapGet("/openapi/v1.json", () => Results.Ok(new { openapi = "3.0.3", info = new { title = "Pathway API", version = "v1" } })).RequireRateLimiting(RateLimitPolicies.PublicRead);
    }

    private static async Task<IResult> Ready(IServiceProvider services, CancellationToken cancellationToken)
    {
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Problem("The progress database is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        return await database.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ready" })
            : Results.Problem("The progress database is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
