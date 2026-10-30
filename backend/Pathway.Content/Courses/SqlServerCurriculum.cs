namespace Pathway.Content;

static partial class Curriculum
{
    // SQL Server: T-SQL and the database engine. docs/sqlserver-curriculum.md has the authoring rules.
    // Lessons are declared as specs in SqlServer*.cs (one file per module) and assembled here. Each spec
    // names the earlier lessons it builds on; the "What you need first" section is generated from that
    // list, so the text and CurriculumInvariantTests can't drift apart. Code exercises are graded by the
    // Python graders in sandbox-broker/src/sql-graders (one file per lesson slug), which parse the
    // learner's T-SQL and run it on SQLite; engine-specific topics are local labs.

    internal sealed record SqlLessonSpec(
        string Slug,
        string Module,
        string Title,
        string Subtitle,
        string Concept,
        (string Slug, string Why)[] Needs,
        string Body,
        string Example,
        Exercise Exercise,
        VersionStamp? Stamp = null);

    // Expression-bodied so it is safe to read from any static initializer (see TailwindCurriculum).
    private static VersionStamp SqlServerStamp => new("T-SQL (SQL Server 2025)", "SQL Server Developer · SSMS", "2026-10-30", "https://learn.microsoft.com/en-us/sql/t-sql/language-reference");

    private static Exercise SqlCode(string title, string prompt, string[] requirements, string starter, string hint, string[] tests) =>
        new(ExerciseKind.Code, title, prompt, requirements, starter, null, [], hint, tests);

    private static Exercise SqlChoice(string title, string prompt, string correct, Choice[] choices, string hint, string test) =>
        new(ExerciseKind.MultipleChoice, title, prompt, ["Choose the best answer"], null, correct, choices, hint, [test]);

    private static Exercise SqlLab(string title, string prompt, string[] evidence, string hint) =>
        new(ExerciseKind.Presentation, title, prompt, evidence, null, null, [], hint, []);

    internal static SqlLessonSpec[] SqlServerLessonSpecs() =>
    [
        .. SqlServerFoundationsSpecs(),
        .. SqlServerQueryingSpecs(),
        .. SqlServerCombiningSpecs(),
    ];

    /// <summary>Each lesson's declared prerequisites, in course order (used by the invariant tests).</summary>
    internal static (string Slug, string[] Needs)[] SqlServerPrerequisites() =>
        SqlServerLessonSpecs().Select(spec => (spec.Slug, spec.Needs.Select(need => need.Slug).ToArray())).ToArray();

    private static Lesson[] BuildSqlServerLessons()
    {
        var specs = SqlServerLessonSpecs();
        var positions = specs.Select((spec, index) => (spec, index)).ToDictionary(item => item.spec.Slug, item => (Order: item.index + 1, item.spec.Title), StringComparer.Ordinal);
        var stamp = SqlServerStamp;
        var lessons = specs.Select((spec, index) => new Lesson(
            spec.Slug,
            spec.Module,
            index + 1,
            spec.Title,
            spec.Subtitle,
            spec.Concept,
            SqlServerPrerequisiteSection(spec, positions) + spec.Body.Trim(),
            spec.Example.Trim(),
            spec.Exercise,
            null,
            spec.Stamp ?? stamp));
        return NormalizeLessons(lessons);
    }

    private static string SqlServerPrerequisiteSection(SqlLessonSpec spec, IReadOnlyDictionary<string, (int Order, string Title)> positions)
    {
        if (spec.Needs.Length == 0)
            return "## What you need first\nNothing from earlier lessons. This lesson starts from zero.\n\n";
        var lines = spec.Needs.Select(need =>
        {
            // An unknown slug is a broken spec; fail at startup rather than render a dead reference.
            if (!positions.TryGetValue(need.Slug, out var position))
                throw new InvalidOperationException($"{spec.Slug} needs unknown lesson '{need.Slug}'.");
            return $"- **Lesson {position.Order}, {position.Title}:** {need.Why}";
        });
        return $"## What you need first\n{string.Join('\n', lines)}\n\n";
    }
}
