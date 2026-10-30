using System.Globalization;

namespace Pathway.Domain;

/// <summary>Grades the exercise kinds that are checked in-process: multiple choice and numeric answers.</summary>
public static class AnswerGrader
{
    public static ValidationResult Choice(Lesson lesson, string? answer)
    {
        var passed = string.Equals(answer, lesson.Exercise.CorrectAnswer, StringComparison.Ordinal);
        return new ValidationResult(passed, passed ? 1 : 0, 1, passed ? "That’s right. You’ve got the idea." : "Not quite—re-read the example and try again.", passed ? lesson.NextSlug : null);
    }
    public static ValidationResult Numeric(Lesson lesson, string? answer, string? submittedUnit)
    {
        var exercise = lesson.Exercise;
        if (exercise.ExpectedNumeric is null || exercise.Tolerance is null)
            return new ValidationResult(false, 0, 1, "This numeric exercise is not configured correctly.", null);

        if (!double.TryParse(answer, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return new ValidationResult(false, 0, 1, "Enter a numeric value, then check the unit.", null);

        var unit = string.IsNullOrWhiteSpace(submittedUnit) ? exercise.Unit : submittedUnit.Trim();
        if (!string.IsNullOrWhiteSpace(exercise.Unit) && !string.Equals(unit, exercise.Unit, StringComparison.OrdinalIgnoreCase))
        {
            if (exercise.UnitConversions is null || !exercise.UnitConversions.TryGetValue(unit ?? string.Empty, out var factor))
                return new ValidationResult(false, 0, 1, $"Use {exercise.Unit} or one of the accepted equivalent units.", null);
            value *= factor;
        }

        var passed = Math.Abs(value - exercise.ExpectedNumeric.Value) <= exercise.Tolerance.Value;
        return new ValidationResult(
            passed,
            passed ? 1 : 0,
            1,
            passed ? "Correct. Your calculation is within the accepted engineering tolerance." : exercise.Hint,
            passed ? lesson.NextSlug : null,
            null,
            passed ? exercise.WorkedSolution : null);
    }
}
