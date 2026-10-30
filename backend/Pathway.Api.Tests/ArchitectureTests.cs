using System.Reflection;
using Xunit;

namespace Pathway.Api.Tests;

/// <summary>
/// Enforces the dependency direction between backend projects. The compiler only records a
/// reference to another assembly when code actually uses it, so these checks fail as soon as a
/// type from a forbidden layer is used, not just when a project reference is added.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(Lesson).Assembly;
    private static readonly Assembly Content = typeof(CourseCatalog).Assembly;
    private static readonly Assembly Infrastructure = typeof(ProgressDbContext).Assembly;

    private static string[] ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty).ToArray();

    [Fact]
    public void Domain_depends_on_no_other_pathway_project_or_framework()
    {
        var references = ReferencedNames(Domain);
        Assert.DoesNotContain(references, name => name.StartsWith("Pathway.", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Npgsql", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal));
    }

    [Fact]
    public void Content_depends_only_on_domain()
    {
        var references = ReferencedNames(Content);
        Assert.Contains("Pathway.Domain", references);
        Assert.DoesNotContain(references, name => name is "Pathway.Infrastructure" or "Pathway.Api");
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_content_or_the_web_host()
    {
        var references = ReferencedNames(Infrastructure);
        Assert.DoesNotContain(references, name => name is "Pathway.Content" or "Pathway.Api");
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_api_endpoint_class_lives_in_a_feature_folder()
    {
        // Vertical slices: endpoint mapping belongs under Pathway.Api.Features.<Feature>.
        var endpointTypes = typeof(Pathway.Api.Hosting.RateLimitPolicies).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Endpoints", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(endpointTypes);
        Assert.All(endpointTypes, type => Assert.StartsWith("Pathway.Api.Features.", type.Namespace));
    }
}
