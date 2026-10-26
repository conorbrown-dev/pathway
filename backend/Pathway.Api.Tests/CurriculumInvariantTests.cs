using Xunit;

namespace Pathway.Api.Tests;

/// <summary>
/// Structural invariants every published course must satisfy. These replace per-course magic
/// numbers: adding or reordering lessons should not require editing tests, but malformed
/// lesson data (missing stamps, broken ordering, dangling links) must fail the build.
/// </summary>
public class CurriculumInvariantTests
{
    public static TheoryData<string> PublishedCourseIds()
    {
        var data = new TheoryData<string>();
        foreach (var item in Curriculum.Catalog.Where(item => item.Available)) data.Add(item.Id);
        return data;
    }

    private static Lesson[] LessonsOf(string courseId) =>
        Curriculum.Courses[courseId].Modules
            .SelectMany(module => module.Lessons)
            .OrderBy(summary => summary.Order)
            .Select(summary => Curriculum.BySlug[summary.Slug])
            .ToArray();

    [Fact]
    public void Every_available_catalog_entry_resolves_to_a_course()
    {
        foreach (var item in Curriculum.Catalog.Where(item => item.Available))
            Assert.True(Curriculum.Courses.ContainsKey(item.Id), $"Catalog course '{item.Id}' has no course definition.");
    }

    [Theory]
    [MemberData(nameof(PublishedCourseIds))]
    public void Every_lesson_summary_resolves_to_a_lesson(string courseId)
    {
        var missing = Curriculum.Courses[courseId].Modules
            .SelectMany(module => module.Lessons)
            .Where(summary => !Curriculum.BySlug.ContainsKey(summary.Slug))
            .Select(summary => summary.Slug)
            .ToArray();
        Assert.True(missing.Length == 0, $"{courseId}: unresolvable lessons {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(PublishedCourseIds))]
    public void Every_lesson_has_a_version_stamp_exercise_and_teaching_copy(string courseId)
    {
        foreach (var lesson in LessonsOf(courseId))
        {
            Assert.True(lesson.Version is not null, $"{lesson.Slug}: Version is null (check static initialization order).");
            Assert.False(string.IsNullOrWhiteSpace(lesson.Version!.SourceUrl), $"{lesson.Slug}: version stamp has no source URL.");
            Assert.True(lesson.Exercise is not null, $"{lesson.Slug}: Exercise is null.");
            Assert.False(string.IsNullOrWhiteSpace(lesson.Title), $"{lesson.Slug}: missing title.");
            Assert.False(string.IsNullOrWhiteSpace(lesson.Concept), $"{lesson.Slug}: missing key idea (Concept).");
            Assert.False(string.IsNullOrWhiteSpace(lesson.Body), $"{lesson.Slug}: missing body.");
        }
    }

    [Theory]
    [MemberData(nameof(PublishedCourseIds))]
    public void Lesson_order_is_contiguous_from_one(string courseId)
    {
        var orders = LessonsOf(courseId).Select(lesson => lesson.Order).ToArray();
        Assert.Equal(Enumerable.Range(1, orders.Length), orders);
    }

    [Theory]
    [MemberData(nameof(PublishedCourseIds))]
    public void Next_links_stay_inside_the_course(string courseId)
    {
        var lessons = LessonsOf(courseId);
        var slugs = lessons.Select(lesson => lesson.Slug).ToHashSet(StringComparer.Ordinal);
        foreach (var lesson in lessons.Where(lesson => lesson.NextSlug is not null))
            Assert.True(slugs.Contains(lesson.NextSlug!), $"{courseId}/{lesson.Slug}: NextSlug '{lesson.NextSlug}' is outside the course.");
    }

    [Theory]
    [MemberData(nameof(PublishedCourseIds))]
    public void Multiple_choice_answer_keys_name_one_of_the_choices(string courseId)
    {
        foreach (var lesson in LessonsOf(courseId).Where(lesson => lesson.Exercise.Kind == ExerciseKind.MultipleChoice))
        {
            Assert.True(lesson.Exercise.Choices.Length > 1, $"{lesson.Slug}: a multiple-choice exercise needs at least two choices.");
            Assert.Contains(lesson.Exercise.CorrectAnswer, lesson.Exercise.Choices.Select(choice => choice.Id));
        }
    }

    [Fact]
    public void Public_lesson_json_never_contains_answer_data()
    {
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        foreach (var lesson in Curriculum.BySlug.Values)
        {
            var json = System.Text.Json.JsonSerializer.SerializeToNode(lesson, options)!["exercise"]!.AsObject();
            foreach (var field in new[] { "correctAnswer", "expectedNumeric", "tolerance", "workedSolution" })
                Assert.False(json.ContainsKey(field), $"{lesson.Slug}: exercise.{field} is serialized in the public lesson response.");
        }
    }

    [Fact]
    public void Tailwind_lessons_carry_the_tailwind_stamp_and_distinct_key_ideas()
    {
        // Regression: TailwindCurriculum.cs once omitted the Concept argument (CS7036) and its
        // version stamp was read before initialization, yielding null for every lesson.
        var lessons = LessonsOf("tailwind-css");
        Assert.NotEmpty(lessons);
        Assert.All(lessons, lesson =>
        {
            Assert.Equal("Tailwind CSS", lesson.Version.Language);
            Assert.NotEqual(lesson.Subtitle, lesson.Concept);
        });
    }

    [Fact]
    public void Precalculus_lessons_tie_math_to_situations_and_explain_every_step()
    {
        // Authoring rule (docs/precalculus-curriculum.md): every formula is anchored to a real or
        // game situation, every letter is named as a variable or constant, and graded answers
        // come with a plain-words worked solution and a real unit.
        var lessons = LessonsOf("precalculus");
        Assert.NotEmpty(lessons);
        Assert.All(lessons, lesson =>
        {
            Assert.Equal("Precalculus", lesson.Version.Language);
            Assert.NotEqual(lesson.Subtitle, lesson.Concept);
            Assert.Contains("Where you'll see it", lesson.Body);
            Assert.Contains("stand for", lesson.Body);
            Assert.True(lesson.Exercise.Kind is ExerciseKind.Numeric or ExerciseKind.MultipleChoice, $"{lesson.Slug}: unexpected exercise kind.");
            if (lesson.Exercise.Kind == ExerciseKind.Numeric)
            {
                Assert.False(string.IsNullOrWhiteSpace(lesson.Exercise.Unit), $"{lesson.Slug}: numeric answer has no unit.");
                Assert.False(string.IsNullOrWhiteSpace(lesson.Exercise.WorkedSolution), $"{lesson.Slug}: numeric answer has no worked solution.");
                Assert.True(lesson.Exercise.ExpectedNumeric is >= 0d, $"{lesson.Slug}: negative answers are awkward on mobile decimal keypads.");
            }
        });
    }

    [Fact]
    public void Llm_lessons_follow_the_authoring_rules()
    {
        // docs/llm-curriculum.md: prerequisites first, every symbol named, and an
        // "Explain it to your team" section that builds toward the final presentation.
        var lessons = LessonsOf("ai-llm-engineering");
        Assert.NotEmpty(lessons);
        Assert.All(lessons, lesson =>
        {
            Assert.StartsWith("Python", lesson.Version.Language);
            Assert.NotEqual(lesson.Subtitle, lesson.Concept);
            Assert.StartsWith("## What you need first", lesson.Body);
            Assert.True(lesson.Body.Contains("## What the symbols stand for") || lesson.Body.Contains("## What the words mean"), $"{lesson.Slug}: name every symbol or term in a \"What the symbols stand for\" or \"What the words mean\" section.");
            Assert.Contains("## Explain it to your team", lesson.Body);
            if (lesson.Exercise.Kind == ExerciseKind.Code)
            {
                Assert.False(string.IsNullOrWhiteSpace(lesson.Exercise.StarterCode), $"{lesson.Slug}: code exercise has no starter code.");
                Assert.NotEmpty(lesson.Exercise.Requirements);
            }
        });
    }

    [Fact]
    public void Llm_lessons_only_build_on_earlier_lessons()
    {
        // The course promise: no concept is used before the lesson that teaches it.
        var order = LessonsOf("ai-llm-engineering").ToDictionary(lesson => lesson.Slug, lesson => lesson.Order);
        foreach (var (slug, needs) in Curriculum.LlmPrerequisites())
        {
            Assert.True(order.ContainsKey(slug), $"{slug} is declared but not in the course.");
            foreach (var need in needs)
            {
                Assert.True(order.TryGetValue(need, out var needOrder), $"{slug} needs '{need}', which is not in the course.");
                Assert.True(needOrder < order[slug], $"{slug} (lesson {order[slug]}) needs '{need}' (lesson {needOrder}), which comes later.");
            }
        }
        // Every lesson after the first builds on something.
        Assert.All(Curriculum.LlmPrerequisites().Skip(1), entry => Assert.NotEmpty(entry.Needs));
    }

    [Fact]
    public void Every_Llm_code_exercise_has_a_python_grader_and_every_grader_has_a_lesson()
    {
        var graders = new DirectoryInfo(Path.GetDirectoryName(FindRepositoryFile("sandbox-broker/src/llm-graders/_harness.py"))!)
            .GetFiles("llm-*.py")
            .Select(file => Path.GetFileNameWithoutExtension(file.Name))
            .ToHashSet(StringComparer.Ordinal);
        var codeLessons = LessonsOf("ai-llm-engineering").Where(lesson => lesson.Exercise.Kind == ExerciseKind.Code).Select(lesson => lesson.Slug).ToHashSet(StringComparer.Ordinal);
        var missing = codeLessons.Except(graders).ToArray();
        var orphaned = graders.Except(codeLessons).ToArray();
        Assert.True(missing.Length == 0, $"LLM code exercises without a grader in sandbox-broker/src/llm-graders: {string.Join(", ", missing)}");
        Assert.True(orphaned.Length == 0, $"Graders without a matching code lesson: {string.Join(", ", orphaned)}");
    }

    [Theory]
    [InlineData("rust-hello-functions")]
    [InlineData("rust-bindings-types")]
    [InlineData("rust-control-flow")]
    [InlineData("rust-collections-strings")]
    [InlineData("rust-option-result")]
    [InlineData("rust-debugging-tests")]
    [InlineData("rust-crates-modules")]
    [InlineData("rust-cargo-quality")]
    [InlineData("rust-stack-heap-layout")]
    [InlineData("rust-ownership")]
    [InlineData("rust-borrowing-references")]
    [InlineData("rust-lifetimes")]
    [InlineData("rust-cli-args-env")]
    [InlineData("rust-stdio-buffering")]
    [InlineData("rust-os-files-syscalls")]
    [InlineData("rust-processes-signals")]
    [InlineData("rust-serde-formats")]
    [InlineData("rust-cli-project")]
    [InlineData("rust-net-tcp-basics")]
    [InlineData("rust-net-framing")]
    [InlineData("rust-net-concurrency-timeouts")]
    [InlineData("rust-net-tokio-kv")]
    [InlineData("rust-bytes-endianness")]
    [InlineData("rust-slices-buffers")]
    [InlineData("rust-binary-parser")]
    [InlineData("rust-perf-cache-benchmark")]
    [InlineData("rust-memory-addresses-raw")]
    [InlineData("rust-unsafe-ffi")]
    [InlineData("rust-atomics-locks")]
    [InlineData("rust-macros")]
    [InlineData("rust-structured-concurrency")]
    [InlineData("rust-testing-properties")]
    [InlineData("rust-systems-wasm-embedded")]
    [InlineData("rust-systems-capstone")]
    [InlineData("rust-structs-enums")]
    [InlineData("rust-traits-generics")]
    [InlineData("rust-errors-panics")]
    [InlineData("rust-iterators-closures")]
    [InlineData("rust-smart-pointers")]
    [InlineData("rust-concurrency-send-sync")]
    [InlineData("rust-async-tokio")]
    public void Authored_Rust_lessons_replace_their_templates(string slug)
    {
        var lesson = Curriculum.BySlug[slug];
        Assert.DoesNotContain("Apply it in a small project, explain the tradeoff", lesson.Body);
        Assert.NotEqual(lesson.Subtitle, lesson.Concept);
        Assert.Contains("```rust", lesson.Body);
        Assert.Equal(ExerciseKind.Code, lesson.Exercise.Kind);
        Assert.False(string.IsNullOrWhiteSpace(lesson.Exercise.StarterCode), $"{slug}: code exercise has no starter code.");
        Assert.NotEmpty(lesson.Exercise.Requirements);
    }

    [Fact]
    public void Every_Rust_code_exercise_has_a_sandbox_fixture()
    {
        // Production grades code only through the sandbox broker. A code lesson with no fixture
        // can never be passed, which blocks the course for everyone (audit finding C3).
        var evaluator = File.ReadAllText(FindRepositoryFile("sandbox-broker/src/evaluator.mjs"));
        var missing = LessonsOf("rust-systems")
            .Where(lesson => lesson.Exercise.Kind == ExerciseKind.Code)
            .Where(lesson => !evaluator.Contains($"case '{lesson.Slug}':", StringComparison.Ordinal))
            .Select(lesson => lesson.Slug)
            .ToArray();
        Assert.True(missing.Length == 0, $"Rust code exercises without a broker fixture: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Rust_foundations_checkpoint_answers_are_choices_and_reviews_are_real_lessons()
    {
        var assessment = AssessmentCatalog.Get("rust-systems", "rust-foundations");
        Assert.NotNull(assessment);
        var slugs = LessonsOf("rust-systems").Select(lesson => lesson.Slug).ToHashSet();
        Assert.True(assessment!.Questions.Length >= 8, "The foundations checkpoint should be sized to its module.");
        Assert.All(assessment.Questions, question =>
        {
            Assert.Contains(question.CorrectAnswer, question.Choices);
            Assert.True(slugs.Contains(question.ReviewLessonSlug), $"{question.Id}: review lesson '{question.ReviewLessonSlug}' is not in the Rust course.");
        });
    }

    private static string FindRepositoryFile(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException($"Could not find {relativePath} above {AppContext.BaseDirectory}.");
    }
}
