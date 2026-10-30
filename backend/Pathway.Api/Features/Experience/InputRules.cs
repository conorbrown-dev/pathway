namespace Pathway.Api.Features.Experience;

/// <summary>Validation shared by the learning-experience features for learner-supplied text, links and paths.</summary>
public static class InputRules
{
    public static bool IsCommunityTextValid(string? text, int max) => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= max && !ContainsSecret(text);
    public static bool IsSafeProjectPath(string? path) => !string.IsNullOrWhiteSpace(path) && path.Length <= 260 && !path.StartsWith("/", StringComparison.Ordinal) && !path.Contains('\\') && path.Split('/').All(segment => segment.Length > 0 && segment is not "." and not ".." && !segment.Contains('\0'));
    public static bool IsSafeHttpsUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) && uri.Host.Length > 0 && value!.Length <= 2_000;
    public static bool IsOptionalSafeHttpsUrl(string? value) => string.IsNullOrWhiteSpace(value) || IsSafeHttpsUrl(value);
    public static bool ContainsSecret(string text) => text.Contains("BEGIN PRIVATE KEY", StringComparison.OrdinalIgnoreCase) || text.Contains("password=", StringComparison.OrdinalIgnoreCase) || text.Contains("api_key", StringComparison.OrdinalIgnoreCase) || text.Contains("authorization: bearer", StringComparison.OrdinalIgnoreCase);
}
