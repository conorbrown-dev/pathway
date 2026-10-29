using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Pathway.Api.Tests;

public class MathMasteryTests
{
    [Fact]
    public void Every_published_math_lesson_has_eight_varied_answerable_questions()
    {
        var slugs = CourseCatalog.Courses.Values
            .Where(course => course.LanguageId == "math")
            .SelectMany(course => course.Modules)
            .SelectMany(module => module.Lessons)
            .Select(lesson => lesson.Slug)
            .ToArray();
        Assert.True(slugs.Length >= 94, "All three math tracks must be covered.");
        foreach (var slug in slugs)
        {
            Assert.True(MathMasteryCatalog.IsMathLesson(slug));
            for (var seed = 1; seed <= 6; seed++)
            {
                var questions = MathMasteryCatalog.Create(slug, seed);
                Assert.Equal(MathMasteryCatalog.QuestionCount, questions.Length);
                Assert.Equal(questions.Length, questions.Select(question => question.Prompt).Distinct().Count());
                Assert.Equal(4, questions.Select(question => question.Skill).Distinct().Count());
                Assert.Equal(questions.Length, questions.Select(question => question.Id).Distinct().Count());
                Assert.All(questions, question =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(question.Prompt));
                    if (question.Kind == "Numeric")
                    {
                        Assert.NotNull(question.Expected);
                        Assert.True(double.IsFinite(question.Expected!.Value));
                        Assert.True(MathMasteryCatalog.Correct(question, question.Expected.Value.ToString("R", CultureInfo.InvariantCulture)));
                        Assert.False(MathMasteryCatalog.Correct(question, (question.Expected.Value + 100).ToString("R", CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        Assert.Equal("Choice", question.Kind);
                        Assert.Equal(3, question.Choices.Distinct().Count());
                        Assert.Contains(question.CorrectChoice, question.Choices);
                        Assert.True(MathMasteryCatalog.Correct(question, question.CorrectChoice));
                        Assert.False(MathMasteryCatalog.Correct(question, question.Choices.First(choice => choice != question.CorrectChoice)));
                    }
                    var publicJson = JsonSerializer.Serialize(question.Public());
                    Assert.DoesNotContain("Expected", publicJson);
                    Assert.DoesNotContain("CorrectChoice", publicJson);
                });
            }
        }
    }

    [Fact]
    public void Method_specific_lessons_assess_their_actual_method()
    {
        var perpendicular = MathMasteryCatalog.Create("algebra1-parallel-perpendicular", 15);
        Assert.Contains(perpendicular, question => question.Prompt.Contains("perpendicular slope"));
        var formula = MathMasteryCatalog.Create("algebra1-quadratic-formula", 15);
        Assert.Contains(formula, question => question.Prompt.Contains("discriminant"));
        var circle = MathMasteryCatalog.Create("math-unit-circle", 15);
        Assert.Contains(circle, question => question.Prompt.Contains("unit circle"));
        Assert.Contains(circle, question => question.CorrectChoice == "(-1,0)");
    }

    [Fact]
    public void Math_mastery_requires_seven_of_eight_and_rejects_invalid_lessons()
    {
        Assert.Equal(8, MathMasteryCatalog.QuestionCount);
        Assert.Equal(7, MathMasteryCatalog.RequiredCorrect);
        Assert.False(MathMasteryCatalog.IsMathLesson("git-cli-init"));
        Assert.Throws<ArgumentException>(() => MathMasteryCatalog.Create("git-cli-init", 42));
        var signed = MathMasteryCatalog.Create("prealgebra-signed-arithmetic", 4);
        Assert.Contains(signed, question => question.Expected is < 0);
    }
}
