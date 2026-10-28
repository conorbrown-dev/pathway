using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Pathway.Api.Hosting;

/// <summary>
/// Named rate-limit policies. Signed-in learners are limited by account, guests by their guest ID,
/// and everyone else by IP address. Development raises every limit so local testing is not throttled.
/// </summary>
public static class RateLimitPolicies
{
    public const string PublicRead = "public-read";
    public const string Learner = "learner";
    public const string Submission = "submission";
    public const string Coach = "coach";
    public const string CommunityWrite = "community-write";

    public static IServiceCollection AddPathwayRateLimits(this IServiceCollection services, bool isDevelopment)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                return ValueTask.CompletedTask;
            };
            var developmentLimit = isDevelopment ? 5_000 : (int?)null;
            options.AddPolicy(PublicRead, context => FixedWindow(context, permitLimit: developmentLimit ?? 180));
            options.AddPolicy(Learner, context => LearnerWindow(context, permitLimit: developmentLimit ?? 90));
            options.AddPolicy(Submission, context => LearnerWindow(context, permitLimit: developmentLimit ?? 10));
            options.AddPolicy(Coach, context => FixedWindow(context, permitLimit: developmentLimit ?? 20));
            options.AddPolicy(CommunityWrite, context => FixedWindow(context, permitLimit: developmentLimit ?? 15));
        });
        return services;
    }

    private static RateLimitPartition<string> FixedWindow(HttpContext context, int permitLimit)
    {
        var subject = context.User.FindFirstValue("sub");
        var key = !string.IsNullOrWhiteSpace(subject) ? $"user:{subject}" : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }

    private static RateLimitPartition<string> LearnerWindow(HttpContext context, int permitLimit)
    {
        var subject = context.User.FindFirstValue("sub");
        var guest = LearnerIdentity.GuestId(context);
        var key = !string.IsNullOrWhiteSpace(subject) ? $"user:{subject}" : guest is not null ? $"guest:{guest}" : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }
}
