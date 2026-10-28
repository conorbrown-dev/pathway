namespace Pathway.Domain;

public record CourseCatalogItem(string Id, string Title, string LanguageId, string LanguageVersion, string FrameworkVersion, bool Available);
public record Course(string Id, string Title, string LanguageId, string LanguageVersion, string FrameworkVersion, string LastReviewed, IReadOnlyList<Module> Modules);
public record Module(string Id, string Title, string Level, IReadOnlyList<LessonSummary> Lessons);
public record LessonSummary(string Slug, string Title, int Order);
public record Lesson(string Slug, string Module, int Order, string Title, string Subtitle, string Concept, string Body, string Example, Exercise Exercise, string? NextSlug, VersionStamp Version);
public record VersionStamp(string Language, string Framework, string LastReviewed, string SourceUrl);
