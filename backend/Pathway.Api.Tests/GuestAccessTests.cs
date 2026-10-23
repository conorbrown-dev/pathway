using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Pathway.Api;
using Xunit;

namespace Pathway.Api.Tests;

/// <summary>
/// Boots the API as production does (Keycloak configured) to pin the guest policy:
/// guests can answer in-process exercises and read their progress, while code exercises,
/// which start a sandbox, require sign-in. CI's browser suite runs without Keycloak and
/// cannot cover this.
/// </summary>
public sealed class GuestAccessTests : IClassFixture<GuestAccessTests.KeycloakConfiguredApi>
{
    public sealed class KeycloakConfiguredApi : WebApplicationFactory<Program>
    {
        public KeycloakConfiguredApi()
        {
            // Program reads these before the host is built, so they must be real configuration
            // sources rather than late overrides. No token is ever validated in these tests, so
            // the authority is never contacted.
            Environment.SetEnvironmentVariable("KEYCLOAK_AUTHORITY", "https://keycloak.invalid/realms/pathway");
            Environment.SetEnvironmentVariable("KEYCLOAK_AUDIENCE", "pathway-api");
        }

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) =>
            builder.UseEnvironment("Production");
    }

    private readonly HttpClient client;

    public GuestAccessTests(KeycloakConfiguredApi api)
    {
        client = api.CreateClient();
        client.DefaultRequestHeaders.Add(LearnerIdentity.GuestHeader, $"guest-test-{Guid.NewGuid():N}"[..40]);
    }

    [Fact]
    public async Task Guest_can_submit_a_multiple_choice_answer()
    {
        var response = await client.PostAsJsonAsync("/api/submissions/validate", new { lessonSlug = "computing-machine-model", answer = "memory" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("True", body!["passed"].ToString(), ignoreCase: true);
    }

    [Fact]
    public async Task Guest_code_submission_asks_for_sign_in()
    {
        var response = await client.PostAsJsonAsync("/api/submissions/validate", new { lessonSlug = "foundations-making-decisions", code = "if (age >= 13) { }" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guest_can_read_progress()
    {
        var response = await client.GetAsync("/api/progress");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Learning_experience_endpoints_still_require_an_account()
    {
        var response = await client.GetAsync("/api/experience/assessments/python-web/python-foundations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
