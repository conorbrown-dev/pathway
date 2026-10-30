namespace Pathway.Domain;

public record CodeReview(string Summary, string[] Suggestions);
public record ValidationResult(bool Passed, int PassingTests, int TotalTests, string Feedback, string? NextLessonSlug, CodeReview? CodeReview = null, string? WorkedSolution = null, CircuitReading? CircuitReading = null);
