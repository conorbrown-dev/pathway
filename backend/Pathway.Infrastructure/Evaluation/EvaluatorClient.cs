using System.Net.Http.Json;

namespace Pathway.Infrastructure.Evaluation;

/// <summary>
/// Talks to the private sandbox evaluator that runs learner code and Claude hook scripts.
/// The API itself never executes learner code.
/// </summary>
public sealed class EvaluatorClient(IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "evaluator";
    private const string SharedSecretHeader = "X-Pathway-Runner-Key";

    /// <summary>Sends <paramref name="body"/> as JSON to <c>{baseUrl}/{path}</c>, signed with the shared secret when one is set.</summary>
    public async Task<HttpResponseMessage> PostAsync(string baseUrl, string path, object body, string? sharedSecret, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/{path}")
        {
            Content = JsonContent.Create(body)
        };
        if (!string.IsNullOrWhiteSpace(sharedSecret))
            request.Headers.TryAddWithoutValidation(SharedSecretHeader, sharedSecret);
        return await client.SendAsync(request, cancellationToken);
    }
}

public record EvaluatorRequest(string LessonSlug, string Code);
public record HookEvaluatorRequest(string Event, string Matcher, string Script, string Command);
