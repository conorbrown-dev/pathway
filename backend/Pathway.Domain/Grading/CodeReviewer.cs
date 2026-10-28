namespace Pathway.Domain;

/// <summary>Advisory readability and idiom suggestions for submitted code. Never changes a test result.</summary>
public static class CodeReviewer
{
    public static CodeReview Review(Lesson lesson, string code)
    {
        var suggestions = new List<string>();
        var lines = code.Split('\n');
        if (lines.Any(line => line.Length > 120)) suggestions.Add("Keep lines under roughly 120 characters where practical so code remains easy to scan in reviews and diffs.");

        if (lesson.Slug.StartsWith("react-", StringComparison.Ordinal))
        {
            if (code.Contains("any", StringComparison.Ordinal)) suggestions.Add("Prefer a narrow domain type or unknown plus validation at trust boundaries instead of any.");
            if (code.Contains("useEffect", StringComparison.Ordinal) && code.Contains("set", StringComparison.Ordinal) && !code.Contains("return", StringComparison.Ordinal)) suggestions.Add("Check whether this Effect is true external-system synchronization; derived React state often belongs directly in render, and subscriptions or timers need cleanup.");
            if (code.Contains("index", StringComparison.OrdinalIgnoreCase) && code.Contains("key=", StringComparison.Ordinal)) suggestions.Add("For reorderable business data, prefer a stable domain identifier over an array index key.");
        }
        else if (lesson.Slug.StartsWith("git-", StringComparison.Ordinal))
        {
            if (code.Contains("git add .", StringComparison.Ordinal)) suggestions.Add("Prefer staging the specific paths that belong in the commit when the exercise calls for a focused change.");
            if (code.Contains("--force", StringComparison.Ordinal) && !code.Contains("--force-with-lease", StringComparison.Ordinal)) suggestions.Add("On shared remotes, prefer --force-with-lease over --force when a history rewrite is truly necessary.");
            if (code.Contains("git reset --hard", StringComparison.Ordinal)) suggestions.Add("git reset --hard discards working-tree and index changes. Confirm status and the target commit before using it outside a disposable lab.");
        }
        else if (lesson.Slug.StartsWith("linux-", StringComparison.Ordinal) || lesson.Slug.StartsWith("bash-", StringComparison.Ordinal) || lesson.Slug.StartsWith("vim-", StringComparison.Ordinal))
        {
            if (code.Contains("rm -rf /", StringComparison.OrdinalIgnoreCase)) suggestions.Add("Avoid destructive recursive removal against root-level paths; narrow and inspect the target first.");
            if (code.Contains("$@", StringComparison.Ordinal) && !code.Contains("\"$@\"", StringComparison.Ordinal)) suggestions.Add("Quote \"$@\" so each original argument keeps its boundary.");
            if (code.Contains("chmod 777", StringComparison.OrdinalIgnoreCase)) suggestions.Add("Avoid chmod 777; grant only the owner/group/other permissions actually required.");
            if (code.Contains("for ", StringComparison.OrdinalIgnoreCase) && code.Contains("$(", StringComparison.Ordinal) && !code.Contains("\"$@\"", StringComparison.Ordinal)) suggestions.Add("Avoid iterating command-substitution text when filenames or fields may contain whitespace; preserve argument/record boundaries.");
        }
        else if (lesson.Slug.StartsWith("sqlite-", StringComparison.Ordinal))
        {
            if (code.Contains("SELECT *", StringComparison.OrdinalIgnoreCase)) suggestions.Add("Prefer explicit result columns when the query is part of a durable application contract.");
            if ((code.Contains("UPDATE ", StringComparison.OrdinalIgnoreCase) || code.Contains("DELETE FROM", StringComparison.OrdinalIgnoreCase)) && !code.Contains("WHERE", StringComparison.OrdinalIgnoreCase)) suggestions.Add("Double-check write scope: UPDATE or DELETE without WHERE affects every row.");
            if (code.Contains("AUTOINCREMENT", StringComparison.OrdinalIgnoreCase)) suggestions.Add("Use AUTOINCREMENT only when never reusing historical rowids is a real requirement; INTEGER PRIMARY KEY is usually enough.");
            if (code.Contains("PRAGMA foreign_keys = OFF", StringComparison.OrdinalIgnoreCase)) suggestions.Add("Disabling foreign-key enforcement weakens relationship integrity; keep it enabled unless a controlled migration specifically requires otherwise.");
        }
        else if (lesson.Slug.StartsWith("rust-", StringComparison.Ordinal))
        {
            if (code.Contains(".unwrap()", StringComparison.Ordinal)) suggestions.Add("Avoid `unwrap()` on inputs or I/O paths where failure is possible; propagate, map, or explicitly recover from errors.");
            if (code.Contains("unsafe", StringComparison.Ordinal) && !code.Contains("SAFETY", StringComparison.Ordinal)) suggestions.Add("Document the invariant that makes an unsafe operation sound with a nearby `SAFETY:` comment, and prefer a small safe wrapper.");
            if (code.Contains("tokio::spawn", StringComparison.Ordinal) && !code.Contains("timeout", StringComparison.Ordinal)) suggestions.Add("Spawned async work needs a bounded lifetime and ownership policy; consider timeout, cancellation, and concurrency limits.");
        }
        else if (lesson.Slug.StartsWith("python-", StringComparison.Ordinal))
        {
            if (code.Contains(" == True", StringComparison.Ordinal) || code.Contains(" == False", StringComparison.Ordinal)) suggestions.Add("Prefer direct truthiness (`if value:` or `if not value:`) over comparing a boolean to `True` or `False`.");
            if (code.Contains("except Exception", StringComparison.Ordinal)) suggestions.Add("Catch the narrowest exception you can handle; a broad `except Exception` can hide programming errors.");
            if (code.Contains(" + \"", StringComparison.Ordinal) || code.Contains("\" +", StringComparison.Ordinal)) suggestions.Add("For user-facing string composition, an f-string is usually clearer than chained `+` concatenation.");
            if (code.Contains("def ", StringComparison.Ordinal) && !code.Contains(" -> ", StringComparison.Ordinal)) suggestions.Add("Consider a return type annotation for public functions so readers and type checkers can verify the contract.");
        }
        else
        {
            if (code.Contains("string.Format(", StringComparison.Ordinal)) suggestions.Add("Prefer interpolated strings (`$\"...\"`) to `string.Format` when formatting a small, readable message.");
            if (code.Contains(" == null", StringComparison.Ordinal) || code.Contains(" != null", StringComparison.Ordinal)) suggestions.Add("Consider pattern matching (`is null` / `is not null`) for modern, intention-revealing null checks.");
            if (code.Contains(".Count() > 0", StringComparison.Ordinal)) suggestions.Add("Prefer `.Any()` when you only need to know whether an enumerable contains an item.");
            if (code.Contains("new List<", StringComparison.Ordinal) && code.Contains("{", StringComparison.Ordinal)) suggestions.Add("When targeting modern C#, consider a collection expression (`[]`) where it makes collection initialization simpler.");
        }

        var summary = suggestions.Count == 0
            ? "No readability or idiom concerns detected by the lightweight review. A human reviewer should still evaluate names, domain fit, and edge cases."
            : $"{suggestions.Count} focused suggestion{(suggestions.Count == 1 ? string.Empty : "s")} found. These are advisory and do not change your test result.";
        return new CodeReview(summary, suggestions.ToArray());
    }
}
