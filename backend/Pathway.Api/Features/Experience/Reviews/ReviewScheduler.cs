namespace Pathway.Api.Features.Experience.Reviews;

/// <summary>
/// Puts lessons back in front of a learner. Used when a checkpoint answer is wrong or a capstone
/// or scenario self-review finds a gap: each lesson becomes due now with its interval reset.
/// </summary>
public static class ReviewScheduler
{
    public static async Task FlagForReviewAsync(IEnumerable<string> lessonSlugs, string learnerId, IServiceProvider services, CancellationToken cancellationToken)
    {
        var slugs = lessonSlugs.Where(CourseCatalog.BySlug.ContainsKey).Distinct(StringComparer.Ordinal).ToArray();
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null || slugs.Length == 0) return;
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        foreach (var lessonSlug in slugs)
        {
            var review = await db.ReviewSchedules.SingleOrDefaultAsync(item => item.LearnerId == learnerId && item.LessonSlug == lessonSlug, cancellationToken);
            if (review is null) db.ReviewSchedules.Add(new ReviewSchedule { LearnerId = learnerId, LessonSlug = lessonSlug, IntervalDays = SpacedReview.FirstIntervalDays, LastReviewedAt = DateTimeOffset.UtcNow, DueAt = DateTimeOffset.UtcNow });
            else { review.IntervalDays = SpacedReview.FirstIntervalDays; review.LastReviewedAt = DateTimeOffset.UtcNow; review.DueAt = DateTimeOffset.UtcNow; }
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
