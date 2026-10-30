using Pathway.Api.Hosting;

namespace Pathway.Api.Features.Courses;

/// <summary>Public, read-only course content. Answers stay server-side (see <see cref="Exercise"/>).</summary>
public static class CourseEndpoints
{
    public static void MapCourseEndpoints(this WebApplication app)
    {
        app.MapGet("/api/courses", () => Results.Ok(CourseCatalog.Catalog)).RequireRateLimiting(RateLimitPolicies.PublicRead);
        app.MapGet("/api/courses/{courseId}", (string courseId) => CourseCatalog.Courses.TryGetValue(courseId, out var course) ? Results.Ok(course) : Results.NotFound()).RequireRateLimiting(RateLimitPolicies.PublicRead);
        app.MapGet("/api/lessons/{slug}", (string slug) => CourseCatalog.BySlug.TryGetValue(slug, out var lesson) ? Results.Ok(lesson) : Results.NotFound()).RequireRateLimiting(RateLimitPolicies.PublicRead);
    }
}
