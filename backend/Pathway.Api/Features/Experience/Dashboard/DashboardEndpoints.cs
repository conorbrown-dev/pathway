namespace Pathway.Api.Features.Experience.Dashboard;

/// <summary>Progress overview: lessons completed, current streak, projects and reviews due.</summary>
public static class DashboardEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/dashboard", GetDashboard);
    }

    private static async Task<IResult> GetDashboard(HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        var learnerId = LearnerIdentity.Resolve(context);
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Ok(new DashboardResponse(learnerId, 0, CourseCatalog.BySlug.Count, 0, 0, [], []));
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var completed = await db.Progress.Where(item => item.LearnerId == learnerId).OrderByDescending(item => item.CompletedAt).ToListAsync(cancellationToken);
        var due = await db.ReviewSchedules.Where(item => item.LearnerId == learnerId && item.DueAt <= DateTimeOffset.UtcNow).OrderBy(item => item.DueAt).Select(item => item.LessonSlug).ToArrayAsync(cancellationToken);
        var projectCount = await db.WorkspaceProjects.CountAsync(item => item.LearnerId == learnerId, cancellationToken);
        var streak = CalculateStreak(completed.Select(item => item.CompletedAt));
        var recent = completed.Take(5).Select(item => new RecentCompletion(item.LessonSlug, item.CompletedAt)).ToArray();
        return Results.Ok(new DashboardResponse(learnerId, completed.Count, CourseCatalog.BySlug.Count, streak, projectCount, due, recent));
    }

    private static int CalculateStreak(IEnumerable<DateTimeOffset> dates) { var days = dates.Select(date => DateOnly.FromDateTime(date.UtcDateTime)).Distinct().ToHashSet(); var today = DateOnly.FromDateTime(DateTime.UtcNow); var streak = 0; while (days.Contains(today.AddDays(-streak))) streak++; return streak; }
}

public record DashboardResponse(string LearnerId, int CompletedLessons, int TotalLessons, int CurrentStreakDays, int ProjectCount, string[] DueReviewLessonSlugs, RecentCompletion[] RecentCompletions);
public record RecentCompletion(string LessonSlug, DateTimeOffset CompletedAt);
