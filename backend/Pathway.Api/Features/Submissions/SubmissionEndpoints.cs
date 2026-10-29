using System.Net.Http.Json;
using System.Text;
using Pathway.Api.Hosting;

namespace Pathway.Api.Features.Submissions;

/// <summary>
/// Grades an exercise answer and records the attempt. Choice, numeric and circuit answers are
/// graded in-process; code goes to the sandbox evaluator.
/// </summary>
public static class SubmissionEndpoints
{
    public static void MapSubmissionEndpoints(this WebApplication app, bool authConfigured)
    {
        app.MapPost("/api/submissions/validate", (Submission submission, HttpContext httpContext, IServiceProvider services, EvaluatorClient evaluator, IConfiguration configuration, IHostEnvironment environment, CancellationToken cancellationToken) =>
                Validate(submission, httpContext, services, evaluator, configuration, environment, authConfigured, cancellationToken))
            .RequireRateLimiting(RateLimitPolicies.Submission);
    }

    private static async Task<IResult> Validate(Submission submission, HttpContext httpContext, IServiceProvider services, EvaluatorClient evaluator, IConfiguration configuration, IHostEnvironment environment, bool authConfigured, CancellationToken cancellationToken)
    {
        if (!CourseCatalog.BySlug.TryGetValue(submission.LessonSlug, out var lesson)) return Results.NotFound();
        // Guests may answer every exercise that is checked in-process. Code exercises start a
        // sandbox per submission, so they require an account once authentication is configured.
        if (authConfigured && lesson.Exercise.Kind == ExerciseKind.Code && !LearnerIdentity.IsAuthenticated(httpContext.User))
            return Results.Problem("Sign in to run code exercises. Your guest progress stays on this device.", statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in required");
        if ((submission.Answer?.Length ?? 0) > 200 || (submission.Unit?.Length ?? 0) > 20 || (submission.MeterMode?.Length ?? 0) > 30 || (submission.RedProbe?.Length ?? 0) > 50 || (submission.BlackProbe?.Length ?? 0) > 50 || (submission.Diagnosis?.Length ?? 0) > 100 || Encoding.UTF8.GetByteCount(submission.Code ?? string.Empty) > 50_000)
            return Results.BadRequest(new { message = "Answers must be under 200 characters, units under 20 characters, and code at most 50 KB." });
        var evaluatorUrl = configuration["EVALUATOR_URL"];
        if (lesson.Exercise.Kind == ExerciseKind.Code && !string.IsNullOrWhiteSpace(evaluatorUrl))
        {
            var evaluatorSharedSecret = configuration["EVALUATOR_SHARED_SECRET"];
            if (environment.IsProduction() && string.IsNullOrWhiteSpace(evaluatorSharedSecret))
                return Results.Problem("The code evaluator is not securely configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
            try
            {
                using var runnerResponse = await evaluator.PostAsync(evaluatorUrl, "evaluate", new EvaluatorRequest(submission.LessonSlug, submission.Code ?? string.Empty), evaluatorSharedSecret, cancellationToken);
                if (runnerResponse.IsSuccessStatusCode)
                {
                    var evaluated = await runnerResponse.Content.ReadFromJsonAsync<ValidationResult>(cancellationToken: cancellationToken);
                    if (evaluated is not null)
                    {
                        var reviewed = evaluated with { CodeReview = evaluated.CodeReview ?? CodeReviewer.Review(lesson, submission.Code ?? string.Empty) };
                        await RecordAttempt(services, LearnerIdentity.Resolve(httpContext), submission, reviewed.Passed, cancellationToken);
                        return Results.Ok(reviewed);
                    }
                }
                return Results.Problem("The code evaluator did not return a usable result.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (HttpRequestException)
            {
                return Results.Problem("The code evaluator is temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }
        // Development-only deterministic checks make local lesson authoring fast. In production,
        // set EVALUATOR_URL to a private sandbox worker; never execute learner code in this API.
        if (lesson.Exercise.Kind == ExerciseKind.Code && environment.IsProduction())
            return Results.Ok(new ValidationResult(false, 0, 0, "Automated execution is not configured yet. Your static code review is ready below; a sandbox evaluator is required before code can be marked complete.", null, CodeReviewer.Review(lesson, submission.Code ?? string.Empty)));
        var validation = lesson.Exercise.Kind switch
        {
            ExerciseKind.MultipleChoice => AnswerGrader.Choice(lesson, submission.Answer),
            ExerciseKind.Numeric => AnswerGrader.Numeric(lesson, submission.Answer, submission.Unit),
            ExerciseKind.Circuit => CircuitExerciseValidator.Validate(lesson, submission.MeterMode, submission.RedProbe, submission.BlackProbe, submission.Diagnosis),
            ExerciseKind.Code => LocalAuthoringChecker.Check(lesson, submission.Code ?? string.Empty),
            _ => new ValidationResult(false, 0, 1, "This exercise type is not supported yet.", null)
        };
        var recorded = lesson.Exercise.Kind == ExerciseKind.Circuit
            ? submission with
            {
                Answer = $"meter={submission.MeterMode};red={submission.RedProbe};black={submission.BlackProbe};diagnosis={submission.Diagnosis}"
            }
            : submission;
        await RecordAttempt(services, LearnerIdentity.Resolve(httpContext), recorded, validation.Passed, cancellationToken);
        // Guided math problems are practice, not proof of mastery.
        if (MathMasteryCatalog.IsMathLesson(lesson.Slug))
            return Results.Ok(validation with { NextLessonSlug = null });
        return Results.Ok(validation);
    }

    /// <summary>
    /// Stores the attempt. A pass also marks the lesson complete and schedules (or pushes back) its
    /// spaced review. Anonymous learners and database-less deployments store nothing.
    /// </summary>
    private static async Task RecordAttempt(IServiceProvider services, string learnerId, Submission submission, bool passed, CancellationToken cancellationToken)
    {
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null || learnerId == LearnerIdentity.Anonymous) return;
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        database.Submissions.Add(new LearnerSubmission { LearnerId = learnerId, LessonSlug = submission.LessonSlug, Answer = submission.Answer, Code = submission.Code, Passed = passed });
        if (passed && !MathMasteryCatalog.IsMathLesson(submission.LessonSlug) && !await database.Progress.AnyAsync(item => item.LearnerId == learnerId && item.LessonSlug == submission.LessonSlug, cancellationToken))
            database.Progress.Add(new LearnerProgress { LearnerId = learnerId, LessonSlug = submission.LessonSlug, CompletedAt = DateTimeOffset.UtcNow });
        if (passed && !MathMasteryCatalog.IsMathLesson(submission.LessonSlug))
        {
            var existingReview = await database.ReviewSchedules.SingleOrDefaultAsync(item => item.LearnerId == learnerId && item.LessonSlug == submission.LessonSlug, cancellationToken);
            if (existingReview is null)
                database.ReviewSchedules.Add(new ReviewSchedule { LearnerId = learnerId, LessonSlug = submission.LessonSlug, IntervalDays = SpacedReview.FirstIntervalDays, LastReviewedAt = DateTimeOffset.UtcNow, DueAt = DateTimeOffset.UtcNow.AddDays(SpacedReview.FirstIntervalDays) });
            else
            {
                existingReview.IntervalDays = SpacedReview.NextIntervalDays(existingReview.IntervalDays);
                existingReview.LastReviewedAt = DateTimeOffset.UtcNow;
                existingReview.DueAt = DateTimeOffset.UtcNow.AddDays(existingReview.IntervalDays);
            }
        }
        await database.SaveChangesAsync(cancellationToken);
    }
}

public record Submission(string LessonSlug, string? Answer, string? Code, string? Unit = null, string? MeterMode = null, string? RedProbe = null, string? BlackProbe = null, string? Diagnosis = null);
