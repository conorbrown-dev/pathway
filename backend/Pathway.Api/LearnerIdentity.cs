using System.Security.Claims;

namespace Pathway.Api;

/// <summary>
/// Resolves the storage key for the current learner. Signed-in learners are keyed by their
/// identity provider's subject (Auth0 <c>sub</c>); guests by a client-generated ID sent in <see cref="GuestHeader"/>.
/// Guest keys are always namespaced with <c>guest:</c> so a client-chosen header can never
/// address a signed-in learner's records (for example by sending <c>user:&lt;sub&gt;</c>).
/// </summary>
public static class LearnerIdentity
{
    public const string GuestHeader = "X-Learner-Id";
    public const string Anonymous = "anonymous";

    // Stored keys are limited to 100 characters; "guest:" + 64 leaves headroom.
    private const int MinGuestIdLength = 8;
    private const int MaxGuestIdLength = 64;

    public static bool IsAuthenticated(ClaimsPrincipal user) => !string.IsNullOrWhiteSpace(user.FindFirstValue("sub"));

    public static bool IsValidGuestId(string? value) =>
        value is { Length: >= MinGuestIdLength and <= MaxGuestIdLength }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    public static string? GuestId(HttpContext context)
    {
        var value = context.Request.Headers[GuestHeader].FirstOrDefault();
        return IsValidGuestId(value) ? value : null;
    }

    public static string Resolve(HttpContext context)
    {
        var subject = context.User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(subject)) return $"user:{subject}";
        return GuestId(context) is { } guest ? $"guest:{guest}" : Anonymous;
    }
}
