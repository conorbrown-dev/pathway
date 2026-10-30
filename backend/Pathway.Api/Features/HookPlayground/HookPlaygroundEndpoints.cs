using System.Net.Http.Json;
using System.Text;
using Pathway.Api.Hosting;

namespace Pathway.Api.Features.HookPlayground;

/// <summary>Runs a learner's Claude Code PreToolUse hook script against a simulated command in the sandbox evaluator.</summary>
public static class HookPlaygroundEndpoints
{
    public static void MapHookPlaygroundEndpoints(this WebApplication app)
    {
        app.MapPost("/api/claude-hooks/evaluate", Evaluate).RequireRateLimiting(RateLimitPolicies.Submission);
    }

    private static async Task<IResult> Evaluate(HookPlaygroundRequest request, EvaluatorClient evaluator, IConfiguration configuration, IHostEnvironment environment, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Event, "PreToolUse", StringComparison.Ordinal))
            return Results.BadRequest(new { message = "The Hook Playground currently supports PreToolUse only." });
        if (string.IsNullOrWhiteSpace(request.Matcher) || request.Matcher.Length > 120)
            return Results.BadRequest(new { message = "Matcher must be between 1 and 120 characters." });
        if (string.IsNullOrWhiteSpace(request.Script) || Encoding.UTF8.GetByteCount(request.Script) > 20_000)
            return Results.BadRequest(new { message = "Hook script must be between 1 and 20 KB." });
        if (string.IsNullOrWhiteSpace(request.Command) || request.Command.Length > 1_000)
            return Results.BadRequest(new { message = "Simulated command must be between 1 and 1,000 characters." });

        var evaluatorUrl = configuration["EVALUATOR_URL"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(evaluatorUrl))
            return Results.Problem("The Hook Playground evaluator is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);

        var evaluatorSharedSecret = configuration["EVALUATOR_SHARED_SECRET"];
        if (environment.IsProduction() && string.IsNullOrWhiteSpace(evaluatorSharedSecret))
            return Results.Problem("The Hook Playground evaluator is not securely configured.", statusCode: StatusCodes.Status503ServiceUnavailable);

        try
        {
            using var evaluatorResponse = await evaluator.PostAsync(evaluatorUrl, "evaluate-hook", new HookEvaluatorRequest(request.Event, request.Matcher, request.Script, request.Command), evaluatorSharedSecret, cancellationToken);
            var payload = await evaluatorResponse.Content.ReadFromJsonAsync<HookPlaygroundResult>(cancellationToken: cancellationToken);
            if (evaluatorResponse.IsSuccessStatusCode && payload is not null)
                return Results.Ok(payload);

            var error = await evaluatorResponse.Content.ReadAsStringAsync(cancellationToken);
            return Results.Problem(
                string.IsNullOrWhiteSpace(error) ? "The Hook Playground evaluator rejected the request." : error[..Math.Min(error.Length, 1_000)],
                statusCode: evaluatorResponse.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status503ServiceUnavailable);
        }
        catch (HttpRequestException)
        {
            return Results.Problem("The Hook Playground evaluator is temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}

public record HookPlaygroundRequest(string Event, string Matcher, string Script, string Command);
public record HookPlaygroundResult(bool MatcherMatched, bool Executed, int? ExitCode, string Outcome, string Summary, string? Reason, string Stdout, string Stderr, string InputJson);
