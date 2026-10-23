using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Pathway.Api;
using Xunit;

namespace Pathway.Api.Tests;

public class LearnerIdentityTests
{
    private static DefaultHttpContext Context(string? guestHeader = null, string? subject = null)
    {
        var context = new DefaultHttpContext();
        if (guestHeader is not null) context.Request.Headers[LearnerIdentity.GuestHeader] = guestHeader;
        if (subject is not null) context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], "test"));
        return context;
    }

    [Fact]
    public void Signed_in_learners_are_keyed_by_subject_even_when_a_guest_header_is_sent() =>
        Assert.Equal("user:auth0|abc-123", LearnerIdentity.Resolve(Context("4f1c2d3e-guest", "auth0|abc-123")));

    [Fact]
    public void Guest_ids_are_namespaced() =>
        Assert.Equal("guest:4f1c2d3e-5a6b-7c8d-9e0f-112233445566", LearnerIdentity.Resolve(Context("4f1c2d3e-5a6b-7c8d-9e0f-112233445566")));

    [Theory]
    [InlineData("user:victim-subject")] // must not address a signed-in learner's records
    [InlineData("guest:nested")]
    [InlineData("short")]
    [InlineData("has spaces in it")]
    [InlineData("")]
    public void Malformed_or_spoofing_guest_ids_resolve_to_anonymous(string header) =>
        Assert.Equal(LearnerIdentity.Anonymous, LearnerIdentity.Resolve(Context(header)));

    [Fact]
    public void Overlong_guest_ids_are_rejected() =>
        Assert.Equal(LearnerIdentity.Anonymous, LearnerIdentity.Resolve(Context(new string('a', 65))));

    [Fact]
    public void Missing_identity_is_anonymous() =>
        Assert.Equal(LearnerIdentity.Anonymous, LearnerIdentity.Resolve(Context()));
}
