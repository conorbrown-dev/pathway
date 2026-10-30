using Pathway.Api.Features.Experience.Reviews;

namespace Pathway.Api.Features.Experience.Assessments;

/// <summary>Module-end checkpoints. Wrong answers flag their lessons for review.</summary>
public static class AssessmentEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/assessments/{courseId}/{moduleId}", GetAssessment);
        group.MapPost("/assessments/{courseId}/{moduleId}", SubmitAssessment);
    }

    private static IResult GetAssessment(string courseId, string moduleId)
    {
        var assessment = AssessmentCatalog.Get(courseId, moduleId);
        return assessment is null ? Results.NotFound() : Results.Ok(assessment with { Questions = assessment.Questions.Select(question => question with { CorrectAnswer = null }).ToArray() });
    }

    private static async Task<IResult> SubmitAssessment(string courseId, string moduleId, AssessmentSubmission submission, HttpContext context, IServiceProvider services, CancellationToken cancellationToken)
    {
        var assessment = AssessmentCatalog.Get(courseId, moduleId);
        if (assessment is null) return Results.NotFound();
        var answers = submission.Answers ?? new Dictionary<string, string>();
        var correct = assessment.Questions.Count(question => answers.TryGetValue(question.Id, out var answer) && answer == question.CorrectAnswer);
        var passed = correct >= Math.Ceiling(assessment.Questions.Length * .7);
        var recommended = assessment.Questions.Where(question => !answers.TryGetValue(question.Id, out var answer) || answer != question.CorrectAnswer).Select(question => question.ReviewLessonSlug).Distinct().ToArray();
        await ReviewScheduler.FlagForReviewAsync(recommended, LearnerIdentity.Resolve(context), services, cancellationToken);
        return Results.Ok(new AssessmentResultResponse(passed, correct, assessment.Questions.Length, recommended, passed ? "Module checkpoint passed. Keep the retrieval practice going." : "Review the suggested lessons, then return for another attempt."));
    }
}

public record AssessmentSubmission(Dictionary<string, string>? Answers);
public record AssessmentResultResponse(bool Passed, int CorrectAnswers, int TotalQuestions, string[] RecommendedReviewLessonSlugs, string Feedback);
