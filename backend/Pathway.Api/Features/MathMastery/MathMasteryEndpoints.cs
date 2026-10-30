using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Pathway.Api.Hosting;

namespace Pathway.Api.Features.MathMastery;

/// <summary>
/// A mathematics lesson is mastered only by a graded eight-question attempt.
/// Individual exercise submissions remain formative and never unlock math lessons.
/// Database-backed attempts survive a process restart; the in-memory path supports local dev.
/// </summary>
public static class MathMasteryEndpoints
{
    private static readonly ConcurrentDictionary<Guid, MathMasteryAttempt> LocalAttempts = new();
    private static readonly ConcurrentDictionary<string, byte> LocalMastery = new();
    private const string KeySeparator = "\u001f";

    public static void MapMathMasteryEndpoints(this WebApplication app)
    {
        app.MapPost("/api/math/mastery/{slug}/start", Start).RequireRateLimiting(RateLimitPolicies.Submission);
        app.MapPost("/api/math/mastery/{slug}/{attemptId:guid}/submit", Submit).RequireRateLimiting(RateLimitPolicies.Submission);
    }

    public static string[] LocalMastered(string learnerId) => LocalMastery.Keys
        .Where(key => key.StartsWith(learnerId + KeySeparator, StringComparison.Ordinal))
        .Select(key => key[(learnerId.Length + KeySeparator.Length)..]).ToArray();

    private static async Task<IResult> Start(string slug, HttpContext context, IServiceProvider services, CancellationToken ct)
    {
        if (!MathMasteryCatalog.IsMathLesson(slug)) return Results.NotFound();
        var questions = MathMasteryCatalog.Create(slug, RandomNumberGenerator.GetInt32(int.MaxValue));
        var owner = LearnerIdentity.Resolve(context);
        var attempt = new MathMasteryAttempt
        {
            LearnerId = owner,
            LessonSlug = slug,
            QuestionsJson = JsonSerializer.Serialize(questions)
        };
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null)
            LocalAttempts[attempt.Id] = attempt;
        else
        {
            await using var database = await factory.CreateDbContextAsync(ct);
            database.MathMasteryAttempts.Add(attempt);
            await database.SaveChangesAsync(ct);
        }
        return Results.Ok(new MasteryStartResponse(attempt.Id, MathMasteryCatalog.RequiredCorrect,
            questions.Select(question => question.Public()).ToArray()));
    }

    private static async Task<IResult> Submit(
        string slug, Guid attemptId, MasterySubmission submission,
        HttpContext context, IServiceProvider services, CancellationToken ct)
    {
        if (!MathMasteryCatalog.IsMathLesson(slug)) return Results.NotFound();
        var owner = LearnerIdentity.Resolve(context);
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        ProgressDbContext? database = null;
        try
        {
            MathMasteryAttempt? attempt;
            if (factory is null)
                LocalAttempts.TryGetValue(attemptId, out attempt);
            else
            {
                database = await factory.CreateDbContextAsync(ct);
                attempt = await database.MathMasteryAttempts.FindAsync([attemptId], ct);
            }
            if (attempt is null || attempt.LearnerId != owner || attempt.LessonSlug != slug)
                return Results.NotFound();
            if (attempt.SubmittedAt is not null)
                return Results.Conflict(new { message = "This mastery attempt has already been submitted. Start a fresh attempt." });

            var questions = JsonSerializer.Deserialize<MathMasteryQuestion[]>(attempt.QuestionsJson);
            if (questions is not { Length: MathMasteryCatalog.QuestionCount })
                return Results.Problem("Mastery attempt configuration is invalid.", statusCode: 500);
            var answers = submission.Answers ?? new Dictionary<string, string>();
            if (answers.Count > questions.Length || answers.Any(pair => pair.Key.Length > 40 || pair.Value.Length > 200))
                return Results.BadRequest(new { message = "Invalid answer payload." });
            var correct = 0;
            var missedSkills = new HashSet<string>(StringComparer.Ordinal);
            foreach (var question in questions)
            {
                if (answers.TryGetValue(question.Id, out var answer) && MathMasteryCatalog.Correct(question, answer))
                    correct++;
                else
                    missedSkills.Add(question.Skill);
            }
            var passed = correct >= MathMasteryCatalog.RequiredCorrect;
            attempt.SubmittedAt = DateTimeOffset.UtcNow;
            attempt.CorrectAnswers = correct;
            attempt.Passed = passed;
            if (database is not null)
            {
                if (passed && !await database.Progress.AnyAsync(item => item.LearnerId == owner && item.LessonSlug == slug, ct))
                    database.Progress.Add(new LearnerProgress { LearnerId = owner, LessonSlug = slug, CompletedAt = DateTimeOffset.UtcNow });
                if (passed)
                {
                    var review = await database.ReviewSchedules.SingleOrDefaultAsync(item => item.LearnerId == owner && item.LessonSlug == slug, ct);
                    if (review is null)
                        database.ReviewSchedules.Add(new ReviewSchedule
                        {
                            LearnerId = owner, LessonSlug = slug, IntervalDays = SpacedReview.FirstIntervalDays,
                            LastReviewedAt = DateTimeOffset.UtcNow, DueAt = DateTimeOffset.UtcNow.AddDays(SpacedReview.FirstIntervalDays)
                        });
                    else
                    {
                        review.IntervalDays = SpacedReview.NextIntervalDays(review.IntervalDays);
                        review.LastReviewedAt = DateTimeOffset.UtcNow;
                        review.DueAt = DateTimeOffset.UtcNow.AddDays(review.IntervalDays);
                    }
                }
                await database.SaveChangesAsync(ct);
            }
            else
            {
                LocalAttempts[attemptId] = attempt;
                if (passed) LocalMastery[owner + KeySeparator + slug] = 1;
            }
            return Results.Ok(new MasteryResultResponse(passed, correct, questions.Length,
                MathMasteryCatalog.RequiredCorrect, missedSkills.Order(StringComparer.Ordinal).ToArray(),
                passed ? "Mastery demonstrated. The next lesson is unlocked."
                       : "Review the missed skills and try a new, varied set of questions."));
        }
        finally
        {
            if (database is not null) await database.DisposeAsync();
        }
    }
}
public record MasteryStartResponse(Guid AttemptId, int RequiredCorrect, MathMasteryPublicQuestion[] Questions);
public record MasterySubmission(Dictionary<string, string>? Answers);
public record MasteryResultResponse(bool Passed, int CorrectAnswers, int TotalQuestions, int RequiredCorrect, string[] MissedSkills, string Feedback);
