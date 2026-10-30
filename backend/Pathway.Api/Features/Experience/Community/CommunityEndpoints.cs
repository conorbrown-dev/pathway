using Pathway.Api.Hosting;

namespace Pathway.Api.Features.Experience.Community;

/// <summary>Course discussion posts and replies, plus opt-in peer-review matching.</summary>
public static class CommunityEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/community/{courseId}", GetCommunityPosts);
        group.MapPost("/community/{courseId}", CreateCommunityPost).RequireRateLimiting(RateLimitPolicies.CommunityWrite);
        group.MapGet("/community/{courseId}/peer-matches", GetPeerMatches);
        group.MapPut("/community/{courseId}/peer-profile", SavePeerProfile).RequireRateLimiting(RateLimitPolicies.CommunityWrite);
        group.MapPost("/community/posts/{postId:guid}/replies", CreateCommunityReply).RequireRateLimiting(RateLimitPolicies.CommunityWrite);
    }

    private static async Task<IResult> GetCommunityPosts(string courseId, IServiceProvider services, CancellationToken cancellationToken)
    {
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Ok(Array.Empty<CommunityPostResponse>());
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var posts = await db.CommunityPosts.Where(item => item.CourseId == courseId).OrderByDescending(item => item.CreatedAt).Take(50).ToListAsync(cancellationToken);
        var postIds = posts.Select(item => item.Id).ToArray();
        var replies = await db.CommunityReplies.Where(item => postIds.Contains(item.PostId)).OrderBy(item => item.CreatedAt).ToListAsync(cancellationToken);
        return Results.Ok(posts.Select(post => CommunityEndpoints.ToCommunityResponse(post, replies.Where(reply => reply.PostId == post.Id))));
    }

    private static async Task<IResult> CreateCommunityPost(string courseId, CommunityPostRequest request, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!CourseCatalog.Courses.ContainsKey(courseId) || !InputRules.IsCommunityTextValid(request.Title, 180) || !InputRules.IsCommunityTextValid(request.Body, 10_000)) return Results.BadRequest(new { message = "Provide a clear title and a respectful question or update." });
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Problem("Community persistence requires a database.", statusCode: StatusCodes.Status503ServiceUnavailable);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var post = new CommunityPost { AuthorId = LearnerIdentity.Resolve(context), CourseId = courseId, Title = request.Title!.Trim(), Body = request.Body!.Trim(), NeedsMentor = request.NeedsMentor };
        db.CommunityPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/experience/community/{courseId}", CommunityEndpoints.ToCommunityResponse(post, []));
    }

    private static async Task<IResult> GetPeerMatches(string courseId, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!CourseCatalog.Courses.ContainsKey(courseId)) return Results.NotFound();
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Ok(Array.Empty<PeerMatchResponse>());
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var learnerId = LearnerIdentity.Resolve(context);
        var matches = await db.PeerReviewProfiles.Where(item => item.CourseId == courseId && item.LearnerId != learnerId && item.AvailableForPeerReview).OrderByDescending(item => item.UpdatedAt).Take(10).Select(item => new PeerMatchResponse(item.Id, item.Focus, item.WantsMentorOfficeHours, item.UpdatedAt)).ToArrayAsync(cancellationToken);
        return Results.Ok(matches);
    }

    private static async Task<IResult> SavePeerProfile(string courseId, PeerReviewProfileRequest request, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!CourseCatalog.Courses.ContainsKey(courseId) || !InputRules.IsCommunityTextValid(request.Focus, 500)) return Results.BadRequest(new { message = "Tell peers what you want to review or learn, without sharing private contact details." });
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Problem("Peer matching requires a database.", statusCode: StatusCodes.Status503ServiceUnavailable);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var learnerId = LearnerIdentity.Resolve(context);
        var profile = await db.PeerReviewProfiles.SingleOrDefaultAsync(item => item.LearnerId == learnerId && item.CourseId == courseId, cancellationToken);
        if (profile is null) { profile = new PeerReviewProfile { LearnerId = learnerId, CourseId = courseId, Focus = request.Focus!.Trim() }; db.PeerReviewProfiles.Add(profile); }
        profile.Focus = request.Focus!.Trim(); profile.AvailableForPeerReview = request.AvailableForPeerReview; profile.WantsMentorOfficeHours = request.WantsMentorOfficeHours; profile.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateCommunityReply(Guid postId, CommunityReplyRequest request, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!InputRules.IsCommunityTextValid(request.Body, 10_000)) return Results.BadRequest(new { message = "Provide a respectful, specific reply." });
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Problem("Community persistence requires a database.", statusCode: StatusCodes.Status503ServiceUnavailable);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (!await db.CommunityPosts.AnyAsync(item => item.Id == postId, cancellationToken)) return Results.NotFound();
        var reply = new CommunityReply { PostId = postId, AuthorId = LearnerIdentity.Resolve(context), Body = request.Body!.Trim(), IsMentor = context.User.IsInRole("mentor") };
        db.CommunityReplies.Add(reply);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/experience/community/posts/{postId}/replies", new CommunityReplyResponse(reply.Id, reply.Body, reply.IsMentor, reply.CreatedAt));
    }

    internal static CommunityPostResponse ToCommunityResponse(CommunityPost post, IEnumerable<CommunityReply> replies) => new(post.Id, post.Title, post.Body, post.NeedsMentor, post.CreatedAt, replies.Select(reply => new CommunityReplyResponse(reply.Id, reply.Body, reply.IsMentor, reply.CreatedAt)).ToArray());
}

public record CommunityPostRequest(string? Title, string? Body, bool NeedsMentor);
public record CommunityReplyRequest(string? Body);
public record CommunityPostResponse(Guid Id, string Title, string Body, bool NeedsMentor, DateTimeOffset CreatedAt, CommunityReplyResponse[] Replies);
public record CommunityReplyResponse(Guid Id, string Body, bool IsMentor, DateTimeOffset CreatedAt);
public record PeerReviewProfileRequest(string? Focus, bool AvailableForPeerReview, bool WantsMentorOfficeHours);
public record PeerMatchResponse(Guid Id, string Focus, bool WantsMentorOfficeHours, DateTimeOffset UpdatedAt);
