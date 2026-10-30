using Pathway.Api.Hosting;

namespace Pathway.Api.Features.Experience.Coach;

/// <summary>Socratic coaching for the current lesson. Never hands out a full solution.</summary>
public static class CoachEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/coach", Coach).RequireRateLimiting(RateLimitPolicies.Coach);
    }

    private static async Task<IResult> Coach(CoachRequest request, IConfiguration configuration, CoachModelClient coachModel, CancellationToken cancellationToken)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message) || message.Length > 2_000) return Results.BadRequest(new { message = "Keep coaching prompts between 1 and 2,000 characters." });
        if (InputRules.ContainsSecret(message)) return Results.BadRequest(new { message = "Remove credentials, tokens, connection strings, and personal data before asking for coaching." });
        if (!CourseCatalog.BySlug.TryGetValue(request.LessonSlug, out var lesson)) return Results.NotFound();
        var asksForAnswer = message.Contains("give me the answer", StringComparison.OrdinalIgnoreCase) || message.Contains("solve it", StringComparison.OrdinalIgnoreCase);
        var fallback = asksForAnswer
            ? $"I won’t provide a copy-paste solution. Start from this idea: {lesson.Concept} Try the smallest change that satisfies one requirement, then tell me what result you observe."
            : $"Let’s reason from the contract: {lesson.Concept} Re-read the requirement, identify the input and expected output, then test the smallest hypothesis. Hint: {lesson.Exercise.Hint}";
        var apiKey = configuration["OPENAI_API_KEY"];
        var model = configuration["COACH_MODEL"];
        if (!asksForAnswer && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(model))
        {
            try
            {
                var input = $"You are a Socratic coding coach. Do not give a complete solution, code block, or copy-paste snippet. Give at most three concise reasoning steps and one question. Lesson concept: {lesson.Concept}\nLesson requirements: {string.Join("; ", lesson.Exercise.Requirements)}\nLearner message: {message}";
                var guidance = await coachModel.AskAsync(apiKey, model, input, cancellationToken);
                if (IsSafeGuidance(guidance)) return Results.Ok(new CoachResponse(guidance!, ["No credentials or personal data", "Guidance first; no full solution", "AI response screened for copy-paste code", "Verify each small change with a test"]));
            }
            catch (HttpRequestException) { /* fall back to deterministic coaching without exposing provider failure */ }
        }
        return Results.Ok(new CoachResponse(fallback, ["No credentials or personal data", "Guidance first; no full solution", "Verify each small change with a test"]));
    }

    private static bool IsSafeGuidance(string? guidance) => !string.IsNullOrWhiteSpace(guidance) && guidance.Length <= 1_600 && !guidance.Contains("```", StringComparison.Ordinal) && !guidance.Contains("BEGIN PRIVATE KEY", StringComparison.OrdinalIgnoreCase);
}

public record CoachRequest(string LessonSlug, string? Message);
public record CoachResponse(string Guidance, string[] Guardrails);
