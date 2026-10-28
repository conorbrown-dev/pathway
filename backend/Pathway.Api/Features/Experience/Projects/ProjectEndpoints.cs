using System.Text;

namespace Pathway.Api.Features.Experience.Projects;

/// <summary>Learner workspaces seeded from project templates, with files saved per learner.</summary>
public static class ProjectEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/projects/templates", () => Results.Ok(ProjectTemplates.All));
        group.MapGet("/projects", GetProjects);
        group.MapPost("/projects/{templateId}", CreateProject);
        group.MapGet("/projects/{projectId:guid}", GetProject);
        group.MapPut("/projects/{projectId:guid}/files/{*path}", SaveProjectFile);
    }

    private static async Task<IResult> GetProjects(HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Ok(Array.Empty<ProjectSummaryResponse>());
        var learnerId = LearnerIdentity.Resolve(context);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var projects = await db.WorkspaceProjects.Where(item => item.LearnerId == learnerId).OrderByDescending(item => item.UpdatedAt).ToListAsync(cancellationToken);
        return Results.Ok(projects.Select(item => new ProjectSummaryResponse(item.Id, item.TemplateId, item.Title, item.UpdatedAt)));
    }

    private static async Task<IResult> CreateProject(string templateId, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!ProjectTemplates.ById.TryGetValue(templateId, out var template)) return Results.NotFound();
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Problem("Project persistence requires a database.", statusCode: StatusCodes.Status503ServiceUnavailable);
        var learnerId = LearnerIdentity.Resolve(context);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var project = await db.WorkspaceProjects.SingleOrDefaultAsync(item => item.LearnerId == learnerId && item.TemplateId == templateId, cancellationToken);
        if (project is null)
        {
            project = new WorkspaceProject { LearnerId = learnerId, TemplateId = templateId, Title = template.Title };
            db.WorkspaceProjects.Add(project);
            db.WorkspaceFiles.AddRange(template.Files.Select(file => new WorkspaceFile { ProjectId = project.Id, Path = file.Path, Content = file.Content }));
            await db.SaveChangesAsync(cancellationToken);
        }
        return Results.Ok(await ToProjectResponse(db, project, cancellationToken));
    }

    private static async Task<IResult> GetProject(Guid projectId, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.NotFound();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var project = await db.WorkspaceProjects.SingleOrDefaultAsync(item => item.Id == projectId && item.LearnerId == LearnerIdentity.Resolve(context), cancellationToken);
        return project is null ? Results.NotFound() : Results.Ok(await ToProjectResponse(db, project, cancellationToken));
    }

    private static async Task<IResult> SaveProjectFile(Guid projectId, string path, SaveFileRequest request, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!InputRules.IsSafeProjectPath(path) || string.IsNullOrWhiteSpace(request.Content) || Encoding.UTF8.GetByteCount(request.Content) > 100_000) return Results.BadRequest(new { message = "Use a safe relative path and keep files under 100 KB." });
        var factory = services.GetService<IDbContextFactory<ProgressDbContext>>();
        if (factory is null) return Results.Problem("Project persistence requires a database.", statusCode: StatusCodes.Status503ServiceUnavailable);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var project = await db.WorkspaceProjects.SingleOrDefaultAsync(item => item.Id == projectId && item.LearnerId == LearnerIdentity.Resolve(context), cancellationToken);
        if (project is null) return Results.NotFound();
        var file = await db.WorkspaceFiles.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Path == path, cancellationToken);
        if (file is null) db.WorkspaceFiles.Add(new WorkspaceFile { ProjectId = projectId, Path = path, Content = request.Content });
        else { file.Content = request.Content; file.UpdatedAt = DateTimeOffset.UtcNow; }
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<ProjectResponse> ToProjectResponse(ProgressDbContext db, WorkspaceProject project, CancellationToken cancellationToken) => new(project.Id, project.TemplateId, project.Title, project.UpdatedAt, await db.WorkspaceFiles.Where(item => item.ProjectId == project.Id).OrderBy(item => item.Path).Select(item => new ProjectFileResponse(item.Path, item.Content, item.UpdatedAt)).ToArrayAsync(cancellationToken));
}

public record ProjectSummaryResponse(Guid Id, string TemplateId, string Title, DateTimeOffset UpdatedAt);
public record ProjectResponse(Guid Id, string TemplateId, string Title, DateTimeOffset UpdatedAt, ProjectFileResponse[] Files);
public record ProjectFileResponse(string Path, string Content, DateTimeOffset UpdatedAt);
public record SaveFileRequest(string Content);
