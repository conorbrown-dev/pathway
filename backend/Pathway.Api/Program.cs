using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Pathway.Api.Features.Analytics;
using Pathway.Api.Features.Courses;
using Pathway.Api.Features.Experience;
using Pathway.Api.Features.Health;
using Pathway.Api.Features.HookPlayground;
using Pathway.Api.Features.Progress;
using Pathway.Api.Features.MathMastery;
using Pathway.Api.Features.Submissions;
using Pathway.Api.Hosting;
using Pathway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 512 * 1024);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddPathwayRateLimits(builder.Environment.IsDevelopment());

// Prefer a direct (non-pooled) connection when the host provides one, as Neon/Vercel do.
var databaseUrl = builder.Configuration["DATABASE_URL_UNPOOLED"] ?? builder.Configuration["DATABASE_URL"] ?? builder.Configuration.GetConnectionString("Pathway");
builder.Services.AddPathwayInfrastructure(databaseUrl);

var allowedOrigins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:5173,http://127.0.0.1:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthorization();

// Any OIDC provider that issues JWT access tokens (Auth0 in production). AUTH_AUTHORITY is the
// issuer, e.g. https://<tenant>.us.auth0.com/; AUTH_AUDIENCE is the API identifier.
var authAuthority = builder.Configuration["AUTH_AUTHORITY"];
var authAudience = builder.Configuration["AUTH_AUDIENCE"];
if (builder.Environment.IsProduction() && (string.IsNullOrWhiteSpace(authAuthority) || string.IsNullOrWhiteSpace(authAudience)))
    throw new InvalidOperationException("AUTH_AUTHORITY and AUTH_AUDIENCE are required in production.");
var authConfigured = !string.IsNullOrWhiteSpace(authAuthority) && !string.IsNullOrWhiteSpace(authAudience);
if (authConfigured)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.Authority = authAuthority;
        options.Audience = authAudience;
        options.MapInboundClaims = false;
        // Roles such as "mentor" are read from this claim. Auth0 RBAC with "Add Permissions in the
        // Access Token" emits a flat "permissions" array, which IsInRole can match directly.
        options.TokenValidationParameters.RoleClaimType = builder.Configuration["AUTH_ROLE_CLAIM"] ?? "permissions";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
}

// Behind Vercel (or another proxy) every request arrives from the proxy's address. When
// TRUST_PROXY_HEADERS=true, use X-Forwarded-For/-Proto so per-IP rate limits key on the client.
// Only enable this where the proxy is the sole way to reach the app (as with Vercel Functions).
var trustProxyHeaders = string.Equals(builder.Configuration["TRUST_PROXY_HEADERS"], "true", StringComparison.OrdinalIgnoreCase);
if (trustProxyHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();
if (trustProxyHeaders)
    app.UseForwardedHeaders();
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { message = "An unexpected server error occurred." });
}));
app.UsePathwaySecurityHeaders();
app.UseCors();
app.UseRateLimiter();
if (authConfigured)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProgressDbContext>>();
    await using var context = await database.CreateDbContextAsync();
    await DatabaseSchema.EnsureAsync(context);
}

app.MapAnalyticsEndpoints();
app.MapHealthEndpoints();
app.MapCourseEndpoints();
app.MapProgressEndpoints();
app.MapMathMasteryEndpoints();
app.MapSubmissionEndpoints(authConfigured);
app.MapHookPlaygroundEndpoints();
var experienceEndpoints = app.MapExperienceEndpoints();
// Progress and submissions stay open to guests (keyed by a namespaced guest ID); the richer
// learning-experience endpoints (workspaces, community, coach) still require an account.
if (authConfigured) experienceEndpoints.RequireAuthorization();

// Course content is built on first use; touch it now so malformed content fails the deploy's
// health check instead of the first learner request.
_ = CourseCatalog.Courses.Count;
app.Run();
