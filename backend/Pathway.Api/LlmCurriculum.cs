static partial class Curriculum
{
    // AI & LLMs end to end. docs/llm-curriculum.md has the authoring rules. Lessons are declared as
    // specs in Llm*.cs (one file per module) and assembled here. Each spec names the earlier lessons
    // it builds on; the "What you need first" section is generated from that list, so the text and
    // CurriculumInvariantTests can't drift apart. Code exercises are graded by the Python graders in
    // sandbox-broker/src/llm-graders (one file per lesson slug).

    internal sealed record LlmLessonSpec(
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
    private static VersionStamp LlmStamp => new("Python 3.11 (standard library)", "AI & LLMs end to end", "2026-10-26", "https://docs.python.org/3.11/tutorial/");

    private static VersionStamp LlmLabStamp(string framework, string sourceUrl) => new("Python 3.11 + local tools", framework, "2026-10-26", sourceUrl);

    private static Exercise LlmCode(string title, string prompt, string[] requirements, string starter, string hint, string[] tests) =>
        new(ExerciseKind.Code, title, prompt, requirements, starter, null, [], hint, tests);

    private static Exercise LlmChoice(string title, string prompt, string correct, Choice[] choices, string hint, string test) =>
        new(ExerciseKind.MultipleChoice, title, prompt, ["Choose the best answer"], null, correct, choices, hint, [test]);

    private static Exercise LlmLab(string title, string prompt, string[] evidence, string hint) =>
        new(ExerciseKind.Presentation, title, prompt, evidence, null, null, [], hint, []);

    internal static LlmLessonSpec[] LlmLessonSpecs() =>
    [
        .. LlmFoundationsSpecs(),
        .. LlmMathSpecs(),
        .. LlmLearningSpecs(),
        .. LlmNeuralSpecs(),
        .. LlmLanguageSpecs(),
        .. LlmTransformerSpecs(),
    ];

    /// <summary>Each lesson's declared prerequisites, in course order (used by the invariant tests).</summary>
    internal static (string Slug, string[] Needs)[] LlmPrerequisites() =>
        LlmLessonSpecs().Select(spec => (spec.Slug, spec.Needs.Select(need => need.Slug).ToArray())).ToArray();

    private static Lesson[] BuildLlmLessons()
    {
        var specs = LlmLessonSpecs();
        var positions = specs.Select((spec, index) => (spec, index)).ToDictionary(item => item.spec.Slug, item => (Order: item.index + 1, item.spec.Title), StringComparer.Ordinal);
        var stamp = LlmStamp;
        var lessons = specs.Select((spec, index) => new Lesson(
            spec.Slug,
            spec.Module,
            index + 1,
            spec.Title,
            spec.Subtitle,
            spec.Concept,
            LlmPrerequisiteSection(spec, positions) + spec.Body.Trim(),
            spec.Example.Trim(),
            spec.Exercise,
            null,
            spec.Stamp ?? stamp));
        return NormalizeLessons(lessons);
    }

    private static string LlmPrerequisiteSection(LlmLessonSpec spec, IReadOnlyDictionary<string, (int Order, string Title)> positions)
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
