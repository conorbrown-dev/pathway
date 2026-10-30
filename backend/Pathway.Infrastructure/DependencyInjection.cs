using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pathway.Infrastructure.Coaching;
using Pathway.Infrastructure.Evaluation;
using Pathway.Infrastructure.Persistence;

namespace Pathway.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers outbound HTTP clients and, when a database URL is configured, the Postgres
    /// context factory. Without a database the API still serves content and grades answers;
    /// features that need storage report that it is unavailable.
    /// </summary>
    public static IServiceCollection AddPathwayInfrastructure(this IServiceCollection services, string? databaseUrl)
    {
        services.AddHttpClient(EvaluatorClient.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient(CoachModelClient.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(12));
        services.AddSingleton<EvaluatorClient>();
        services.AddSingleton<CoachModelClient>();
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            services.AddDbContextFactory<ProgressDbContext>(options => options.UseNpgsql(DatabaseConnection.ToNpgsql(databaseUrl)));
        return services;
    }
}
