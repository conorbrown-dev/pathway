namespace Pathway.Domain;

/// <summary>
/// Spaced-repetition rule for lesson reviews. Each successful review doubles the wait before the
/// next one, capped at a month. A lesson flagged as a gap (failed checkpoint, missing capstone
/// evidence) is due again immediately with the interval reset to one day.
/// </summary>
public static class SpacedReview
{
    public const int FirstIntervalDays = 1;
    public const int MaxIntervalDays = 30;

    public static int NextIntervalDays(int currentIntervalDays) => Math.Min(currentIntervalDays * 2, MaxIntervalDays);
}
