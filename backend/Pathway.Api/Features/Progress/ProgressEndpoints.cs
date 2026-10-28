using Pathway.Api.Hosting;

namespace Pathway.Api.Features.Progress;

/// <summary>The lessons the current learner (signed in or guest) has completed, oldest first.</summary>
public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this WebApplication app)
    {
        app.MapGet("/api/progress", GetProgress).RequireRateLimiting(RateLimitPolicies.Learner);
    }

    private static async Task<IResult> GetProgress(HttpContext httpContext, IServiceProvider services, CancellationToken cancellationToken)
    {
        var learnerId = LearnerIdentity.Resolve(httpContext);
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Ok(new LearnerProgressResponse(learnerId, []));
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        var completed = await database.Progress.Where(item => item.LearnerId == learnerId).OrderBy(item => item.CompletedAt).Select(item => item.LessonSlug).ToArrayAsync(cancellationToken);
        return Results.Ok(new LearnerProgressResponse(learnerId, completed));
    }
}

public record LearnerProgressResponse(string LearnerId, string[] CompletedLessonSlugs);
