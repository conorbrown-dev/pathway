using Pathway.Api.Features.Experience.Assessments;
using Pathway.Api.Features.Experience.Career;
using Pathway.Api.Features.Experience.Coach;
using Pathway.Api.Features.Experience.Community;
using Pathway.Api.Features.Experience.Dashboard;
using Pathway.Api.Features.Experience.Projects;
using Pathway.Api.Features.Experience.Reviews;
using Pathway.Api.Hosting;

namespace Pathway.Api.Features.Experience;

/// <summary>
/// The learning-experience features under /api/experience. Each feature folder maps its own routes;
/// this group applies the shared learner rate limit and, when sign-in is configured, requires it.
/// </summary>
public static class ExperienceEndpoints
{
    public static RouteGroupBuilder MapExperienceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/experience");
        group.RequireRateLimiting(RateLimitPolicies.Learner);
        DashboardEndpoints.Map(group);
        ReviewEndpoints.Map(group);
        ProjectEndpoints.Map(group);
        CareerEndpoints.Map(group);
        AssessmentEndpoints.Map(group);
        CoachEndpoints.Map(group);
        CommunityEndpoints.Map(group);
        return group;
    }
}
