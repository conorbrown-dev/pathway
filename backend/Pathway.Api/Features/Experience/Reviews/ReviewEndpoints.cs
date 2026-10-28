namespace Pathway.Api.Features.Experience.Reviews;

/// <summary>Spaced-repetition reviews: what is due now, and marking a review done.</summary>
public static class ReviewEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/reviews", GetDueReviews);
        group.MapPost("/reviews/{lessonSlug}/complete", CompleteReview);
    }

    private static async Task<IResult> GetDueReviews(HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        var learnerId = LearnerIdentity.Resolve(context);
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Ok(Array.Empty<ReviewItemResponse>());
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var schedules = await db.ReviewSchedules.Where(item => item.LearnerId == learnerId && item.DueAt <= DateTimeOffset.UtcNow).OrderBy(item => item.DueAt).ToListAsync(cancellationToken);
        return Results.Ok(schedules.Where(item => CourseCatalog.BySlug.TryGetValue(item.LessonSlug, out _)).Select(item => new ReviewItemResponse(item.LessonSlug, CourseCatalog.BySlug[item.LessonSlug].Title, item.DueAt, item.IntervalDays)));
    }

    private static async Task<IResult> CompleteReview(string lessonSlug, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        var learnerId = LearnerIdentity.Resolve(context);
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null || !CourseCatalog.BySlug.ContainsKey(lessonSlug)) return Results.NotFound();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var review = await db.ReviewSchedules.SingleOrDefaultAsync(item => item.LearnerId == learnerId && item.LessonSlug == lessonSlug, cancellationToken);
        if (review is null) return Results.NotFound();
        review.IntervalDays = SpacedReview.NextIntervalDays(review.IntervalDays);
        review.LastReviewedAt = DateTimeOffset.UtcNow;
        review.DueAt = DateTimeOffset.UtcNow.AddDays(review.IntervalDays);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new ReviewItemResponse(review.LessonSlug, CourseCatalog.BySlug[review.LessonSlug].Title, review.DueAt, review.IntervalDays));
    }
}

public record ReviewItemResponse(string LessonSlug, string Title, DateTimeOffset DueAt, int IntervalDays);
