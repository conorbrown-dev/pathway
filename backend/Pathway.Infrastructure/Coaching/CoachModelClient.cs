using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pathway.Infrastructure.Coaching;

/// <summary>
/// Optional AI coach backed by the OpenAI Responses API. Returns null when the provider answers
/// with an error, so callers fall back to deterministic coaching. Network failures surface as
/// <see cref="HttpRequestException"/>.
/// </summary>
public sealed class CoachModelClient(IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "coach";

    public async Task<string?> AskAsync(string apiKey, string model, string input, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
        {
            Content = JsonContent.Create(new { model, store = false, input })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken), default);
        return ExtractResponseText(document.RootElement);
    }

    private static string? ExtractResponseText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String) return outputText.GetString();
            if (element.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String) return text.GetString();
            foreach (var propertyName in new[] { "output", "content" }) if (element.TryGetProperty(propertyName, out var property)) { var value = ExtractResponseText(property); if (!string.IsNullOrWhiteSpace(value)) return value; }
        }
        if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) { var value = ExtractResponseText(item); if (!string.IsNullOrWhiteSpace(value)) return value; }
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }
}
