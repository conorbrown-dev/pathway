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
}
