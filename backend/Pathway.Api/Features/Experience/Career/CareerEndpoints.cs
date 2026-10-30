using Pathway.Api.Features.Experience.Community;
using Pathway.Api.Features.Experience.Reviews;
using Pathway.Api.Hosting;

namespace Pathway.Api.Features.Experience.Career;

/// <summary>Career readiness: capstone and scenario self-review gates, competency evidence, and outcome check-ins.</summary>
public static class CareerEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/career/{courseId}/capstones", (string courseId) => Results.Ok(CareerReadinessCatalog.CapstonesFor(courseId)));
        group.MapPost("/career/{courseId}/capstones/{capstoneId}/review", ReviewCapstone);
        group.MapPost("/career/{courseId}/capstones/{capstoneId}/review-request", CreateCapstoneReviewRequest).RequireRateLimiting(RateLimitPolicies.CommunityWrite);
        group.MapGet("/career/{courseId}/competencies", GetCompetencies);
        group.MapGet("/career/{courseId}/scenarios", (string courseId) => Results.Ok(CareerReadinessCatalog.ScenariosFor(courseId)));
        group.MapPost("/career/{courseId}/scenarios/{scenarioId}/review", ReviewScenario);
        group.MapGet("/career/{courseId}/outcomes", GetOutcomes);
        group.MapPut("/career/{courseId}/outcomes", SaveOutcomes);
    }

    private static async Task<IResult> GetCompetencies(string courseId, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!CourseCatalog.Courses.TryGetValue(courseId, out var course)) return Results.NotFound();
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        var completed = 0;
        if (factory is not null)
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            completed = await db.Progress.CountAsync(item => item.LearnerId == LearnerIdentity.Resolve(context) && CourseCatalog.BySlug.ContainsKey(item.LessonSlug), cancellationToken);
        }
        var total = course.Modules.Sum(module => module.Lessons.Count);
        var baseline = total == 0 ? 0 : (int)Math.Floor(completed / (double)total * 3);
        return Results.Ok(CareerReadinessCatalog.Competencies.Select(item => new CompetencyEvidenceResponse(item.Id, item.Title, item.Description, Math.Min(item.Evidence.Length, baseline), item.Evidence.Length, item.Evidence)));
    }

    private static async Task<IResult> ReviewCapstone(string courseId, string capstoneId, CapstoneReviewRequest request, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        var capstone = CareerReadinessCatalog.CapstonesFor(courseId).SingleOrDefault(item => item.Id == capstoneId);
        if (capstone is null) return Results.NotFound();
        var evidence = request.Evidence ?? [];
        var missing = new List<string>();
        if (!InputRules.IsSafeHttpsUrl(request.DemoUrl)) missing.Add("A public HTTPS demo link");
        if (!InputRules.IsSafeHttpsUrl(request.ArchitectureUrl)) missing.Add("An HTTPS architecture diagram link");
        if (!InputRules.IsSafeHttpsUrl(request.RepositoryUrl)) missing.Add("An HTTPS repository link");
        foreach (var rubric in capstone.Rubric) if (!evidence.TryGetValue(rubric.Id, out var value) || string.IsNullOrWhiteSpace(value) || value.Trim().Length < 20) missing.Add(rubric.Title);
        var satisfied = capstone.Rubric.Length + 3 - missing.Count;
        var ready = missing.Count == 0;
        var recommendedLessons = CareerReadinessCatalog.RecommendedLessons(courseId, missing);
        await ReviewScheduler.FlagForReviewAsync(recommendedLessons, LearnerIdentity.Resolve(context), services, cancellationToken);
        return Results.Ok(new CapstoneReviewResponse(ready, satisfied, capstone.Rubric.Length + 3, missing.ToArray(), ready ? ["Your submission satisfies the self-review gate. Request rubric-guided peer or mentor review next."] : ["Complete the missing evidence before requesting review.", "Evidence should explain a concrete decision, not merely claim that it exists."], recommendedLessons));
    }

    private static async Task<IResult> ReviewScenario(string courseId, string scenarioId, ScenarioReviewRequest request, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        var scenario = CareerReadinessCatalog.ScenariosFor(courseId).SingleOrDefault(item => item.Id == scenarioId);
        if (scenario is null) return Results.NotFound();
        var responses = request.Responses ?? [];
        var missing = scenario.Deliverables.Where(item => !responses.TryGetValue(item, out var response) || string.IsNullOrWhiteSpace(response) || response.Trim().Length < 60).ToArray();
        var ready = missing.Length == 0;
        var recommendedLessons = CareerReadinessCatalog.RecommendedLessons(courseId, missing);
        await ReviewScheduler.FlagForReviewAsync(recommendedLessons, LearnerIdentity.Resolve(context), services, cancellationToken);
        return Results.Ok(new ScenarioReviewResponse(ready, scenario.Deliverables.Length - missing.Length, scenario.Deliverables.Length, missing, ready
            ? ["This is ready for peer or mentor feedback. Re-read it once for a concrete claim, evidence, and next action in every section.", "Capture the strongest response in your portfolio evidence."]
            : ["Make each response specific: name the evidence, decision, tradeoff, and next action.", "A good engineering response is not a list of buzzwords; it is a defensible decision under constraints."], recommendedLessons));
    }

    private static async Task<IResult> CreateCapstoneReviewRequest(string courseId, string capstoneId, CapstoneReviewRequestPost request, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        var capstone = CareerReadinessCatalog.CapstonesFor(courseId).SingleOrDefault(item => item.Id == capstoneId);
        if (capstone is null) return Results.NotFound();
        var evidence = request.Evidence ?? [];
        var isGateComplete = InputRules.IsSafeHttpsUrl(request.DemoUrl) && InputRules.IsSafeHttpsUrl(request.ArchitectureUrl) && InputRules.IsSafeHttpsUrl(request.RepositoryUrl)
            && capstone.Rubric.All(item => evidence.TryGetValue(item.Id, out var value) && !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 20);
        if (!isGateComplete || !InputRules.IsCommunityTextValid(request.ReviewFocus, 2_000)) return Results.BadRequest(new { message = "Complete the capstone gate and provide a specific review focus before requesting feedback." });
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Problem("Community persistence requires a database.", statusCode: StatusCodes.Status503ServiceUnavailable);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var body = $"Rubric-guided review requested.\n\nDemo: {request.DemoUrl!.Trim()}\nArchitecture: {request.ArchitectureUrl!.Trim()}\nRepository: {request.RepositoryUrl!.Trim()}\n\nReview focus: {request.ReviewFocus!.Trim()}\n\nPlease leave constructive, evidence-based feedback against the requirements, API design, tests, delivery, security, observability, deployment, README, and tradeoffs.";
        var post = new CommunityPost { AuthorId = LearnerIdentity.Resolve(context), CourseId = courseId, Title = $"Review request: {capstone.Title}", Body = body, NeedsMentor = true };
        db.CommunityPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/experience/community/{courseId}", CommunityEndpoints.ToCommunityResponse(post, []));
    }

    private static async Task<IResult> GetOutcomes(string courseId, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!CourseCatalog.Courses.TryGetValue(courseId, out var course)) return Results.NotFound();
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Ok(OutcomeMetricsResponse.Empty(courseId));
        var learnerId = LearnerIdentity.Resolve(context);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var lessonSlugs = course.Modules.SelectMany(module => module.Lessons).Select(lesson => lesson.Slug).ToHashSet(StringComparer.Ordinal);
        var submissions = await db.Submissions.Where(item => item.LearnerId == learnerId && lessonSlugs.Contains(item.LessonSlug)).ToListAsync(cancellationToken);
        var completed = await db.Progress.CountAsync(item => item.LearnerId == learnerId && lessonSlugs.Contains(item.LessonSlug), cancellationToken);
        var checkIn = await db.CareerOutcomeCheckIns.SingleOrDefaultAsync(item => item.LearnerId == learnerId && item.CourseId == courseId, cancellationToken);
        var firstAttempt = submissions.OrderBy(item => item.CreatedAt).Select(item => (DateTimeOffset?)item.CreatedAt).FirstOrDefault();
        return Results.Ok(new OutcomeMetricsResponse(courseId, completed, lessonSlugs.Count, submissions.Count, submissions.Count(item => item.Passed), firstAttempt, checkIn?.InterviewReadiness ?? 0, checkIn?.MentorReadiness ?? 0, checkIn?.JobSearchStage, checkIn?.PortfolioUrl, checkIn?.UpdatedAt));
    }

    private static async Task<IResult> SaveOutcomes(string courseId, OutcomeCheckInRequest request, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!CourseCatalog.Courses.ContainsKey(courseId) || request.InterviewReadiness is < 0 or > 5 || request.MentorReadiness is < 0 or > 5 || !InputRules.IsOptionalSafeHttpsUrl(request.PortfolioUrl) || request.JobSearchStage?.Length > 80) return Results.BadRequest(new { message = "Use readiness scores from 0–5 and an optional safe HTTPS portfolio URL." });
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Problem("Outcome tracking requires a database.", statusCode: StatusCodes.Status503ServiceUnavailable);
        var learnerId = LearnerIdentity.Resolve(context);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var checkIn = await db.CareerOutcomeCheckIns.SingleOrDefaultAsync(item => item.LearnerId == learnerId && item.CourseId == courseId, cancellationToken);
        if (checkIn is null) { checkIn = new CareerOutcomeCheckIn { LearnerId = learnerId, CourseId = courseId }; db.CareerOutcomeCheckIns.Add(checkIn); }
        checkIn.InterviewReadiness = request.InterviewReadiness;
        checkIn.MentorReadiness = request.MentorReadiness;
        checkIn.JobSearchStage = request.JobSearchStage?.Trim();
        checkIn.PortfolioUrl = request.PortfolioUrl?.Trim();
        checkIn.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}

public record CapstoneReviewRequest(string? DemoUrl, string? ArchitectureUrl, string? RepositoryUrl, Dictionary<string, string>? Evidence);
public record CapstoneReviewResponse(bool ReadyForReview, int SatisfiedCriteria, int TotalCriteria, string[] MissingCriteria, string[] Feedback, string[] RecommendedReviewLessonSlugs);
public record CapstoneReviewRequestPost(string? DemoUrl, string? ArchitectureUrl, string? RepositoryUrl, Dictionary<string, string>? Evidence, string? ReviewFocus);
public record CompetencyEvidenceResponse(string Id, string Title, string Description, int EarnedEvidence, int TargetEvidence, string[] Evidence);
public record ScenarioReviewRequest(Dictionary<string, string>? Responses);
public record ScenarioReviewResponse(bool ReadyForFeedback, int SatisfiedDeliverables, int TotalDeliverables, string[] MissingDeliverables, string[] Feedback, string[] RecommendedReviewLessonSlugs);
public record OutcomeCheckInRequest(int InterviewReadiness, int MentorReadiness, string? JobSearchStage, string? PortfolioUrl);
public record OutcomeMetricsResponse(string CourseId, int CompletedLessons, int TotalLessons, int ExerciseAttempts, int PassedAttempts, DateTimeOffset? FirstAttemptAt, int InterviewReadiness, int MentorReadiness, string? JobSearchStage, string? PortfolioUrl, DateTimeOffset? LastCheckInAt)
{
    public static OutcomeMetricsResponse Empty(string courseId) => new(courseId, 0, 0, 0, 0, null, 0, 0, null, null, null);
}
