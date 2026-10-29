namespace Pathway.Content;

public record AssessmentQuestion(string Id, string Prompt, string[] Choices, string? CorrectAnswer, string ReviewLessonSlug);
public record ModuleAssessment(string CourseId, string ModuleId, string Title, AssessmentQuestion[] Questions);

/// <summary>Module-end checkpoints. CorrectAnswer is stripped before a checkpoint is sent to learners.</summary>
public static class AssessmentCatalog
{
    private static readonly ModuleAssessment[] All =
    [
        new("prealgebra", "arithmetic-foundations-1", "Arithmetic foundations checkpoint", [
            new("places", "In 6,205, what does the 2 represent?", ["Two hundreds", "Two tens", "Two thousands"], "Two hundreds", "prealgebra-place-value"),
            new("groups", "What is 7 × 9?", ["63", "16", "79"], "63", "prealgebra-four-operations"),
            new("priority", "Evaluate 2 + 3 × 4.", ["20", "14", "24"], "14", "prealgebra-operation-order"),
            new("factors", "What is the greatest common factor of 12 and 18?", ["6", "3", "36"], "6", "prealgebra-factors-multiples")
        ]),
        new("prealgebra", "fractions-and-rational-numbers-2", "Fractions and rational numbers checkpoint", [
            new("meaning", "Which fraction means three of four equal parts?", ["3/4", "4/3", "1/3"], "3/4", "prealgebra-fraction-meaning"),
            new("equivalent", "Which fraction is equivalent to 2/3?", ["6/9", "4/9", "3/2"], "6/9", "prealgebra-equivalent-fractions"),
            new("add", "What is 1/2 + 1/4?", ["2/6", "3/4", "1/8"], "3/4", "prealgebra-fraction-add-subtract"),
            new("multiply", "What is 1/2 × 2/3?", ["1/3", "3/5", "2/5"], "1/3", "prealgebra-fraction-multiply-divide")
        ]),
        new("prealgebra", "decimals-and-percentages-3", "Decimals and percentages checkpoint", [
            new("fraction-decimal", "What is 1/4 as a decimal?", ["0.4", "0.25", "0.14"], "0.25", "prealgebra-decimals"),
            new("money", "What is 1.5 × 0.2?", ["0.3", "3", "0.03"], "0.3", "prealgebra-decimal-arithmetic"),
            new("rate", "A runner travels 15 miles in 3 hours. What is the unit rate?", ["45 miles/hour", "5 miles/hour", "12 miles/hour"], "5 miles/hour", "prealgebra-ratios-rates"),
            new("scale", "If 2 cups make 4 servings, how many cups make 10 servings?", ["5", "8", "20"], "5", "prealgebra-proportions"),
            new("constant", "A proportional rule is y = 4x. What is its constant of proportionality?", ["4", "0", "x"], "4", "prealgebra-constant-proportionality"),
            new("discount", "What is 10% of $80?", ["$8", "$10", "$72"], "$8", "prealgebra-percent")
        ]),
        new("prealgebra", "signed-numbers-and-powers-4", "Signed numbers and powers checkpoint", [
            new("compare", "Which is greater?", ["−2", "−7", "They are equal"], "−2", "prealgebra-integers"),
            new("subtract", "What is −5 − (−8)?", ["−13", "3", "−3"], "3", "prealgebra-signed-arithmetic"),
            new("power", "What is 3²?", ["6", "9", "8"], "9", "prealgebra-exponents"),
            new("root", "What is √64?", ["8", "32", "−8"], "8", "prealgebra-roots")
        ]),
        new("prealgebra", "algebraic-expressions-5", "Algebraic expressions checkpoint", [
            new("variable", "In 5 + 2n, which quantity changes with n?", ["5", "2n", "The plus sign"], "2n", "prealgebra-variables"),
            new("substitution", "Evaluate 3x + 1 when x = 4.", ["13", "16", "8"], "13", "prealgebra-evaluate-expressions"),
            new("terms", "Simplify 2y + 5y + 3.", ["7y + 3", "10y", "10y + 3"], "7y + 3", "prealgebra-like-terms"),
            new("distribute", "Expand 2(x + 6).", ["2x + 6", "2x + 12", "x + 12"], "2x + 12", "prealgebra-distributive")
        ]),
        new("prealgebra", "equations-and-inequalities-6", "Equations and inequalities checkpoint", [
            new("balance", "If x + 3 = 10, what is x?", ["13", "7", "3"], "7", "prealgebra-equality-balance"),
            new("one-step", "Solve 4x = 32.", ["8", "28", "128"], "8", "prealgebra-one-step"),
            new("two-step", "Solve 2x + 5 = 17.", ["6", "11", "7"], "6", "prealgebra-two-step"),
            new("inequality", "Which value satisfies x ≤ 4?", ["5", "4", "10"], "4", "prealgebra-inequalities")
        ]),
        new("prealgebra", "graphs-and-relationships-7", "Graphs and relationships checkpoint", [
            new("coords", "For (−3, 2), which direction is x?", ["Right", "Left", "Up"], "Left", "prealgebra-coordinate-plane"),
            new("table", "If output = 2n + 3, what is output for n = 4?", ["11", "9", "14"], "11", "prealgebra-tables-patterns"),
            new("slope", "A line rises 8 units over a run of 4 units. What is its slope?", ["2", "4", "12"], "2", "prealgebra-slope")
        ]),
        new("prealgebra", "geometry-and-data-8", "Geometry, data, and probability checkpoint", [
            new("area", "What is the area of a 3 m by 5 m rectangle?", ["15 m²", "16 m", "8 m²"], "15 m²", "prealgebra-area-perimeter"),
            new("volume", "What is the volume of a 2 cm × 3 cm × 4 cm box?", ["9 cm³", "24 cm³", "24 cm²"], "24 cm³", "prealgebra-volume-measurement"),
            new("median", "What is the median of 3, 9, 4?", ["9", "4", "16/3"], "4", "prealgebra-data-probability")
        ]),
        new("prealgebra", "applied-problem-solving-9", "Applied problem solving checkpoint", [
            new("translate", "A $6 entry fee plus $3 per ride r costs what?", ["6r + 3", "6 + 3r", "9r"], "6 + 3r", "prealgebra-word-problems"),
            new("cap-discount", "A $20 item is 25% off. What is the new price?", ["$15", "$5", "$25"], "$15", "prealgebra-capstone"),
            new("budget", "After spending $35 of a $90 budget, how much remains?", ["$55", "$125", "$65"], "$55", "prealgebra-capstone")
        ]),
        new("python-web", "python-foundations", "Python foundations checkpoint", [new("values", "Which collection models unique permission names?", ["list", "set", "tuple"], "set", "python-control-flow-collections"), new("contract", "What should a focused function return?", ["A useful value or explicit None", "Printed output only", "Global mutable state"], "A useful value or explicit None", "python-functions"), new("boundary", "Where should external input be validated?", ["At an application boundary", "Only in templates", "Never"], "At an application boundary", "python-data-models")]),
        new("rust-systems", "rust-foundations", "Rust foundations checkpoint", [new("signature", "A function is declared `fn area(w: u32, h: u32) -> u32 { w * h; }`. Why does it fail to compile?", ["The semicolon turns the last expression into a statement, so the body produces () instead of u32", "Rust requires the return keyword in every function", "u32 values cannot be multiplied without a cast"], "The semicolon turns the last expression into a statement, so the body produces () instead of u32", "rust-hello-functions"), new("overflow", "A u8 counter holds 255 and the code runs `counter + 1` in a debug build. What happens?", ["The counter silently becomes 0", "The counter becomes 256 because Rust widens the type", "The program panics with an overflow error"], "The program panics with an overflow error", "rust-bindings-types"), new("convert", "Which expression turns an i32 that might be 300 into a u8 without silently changing its value?", ["value.to_string().len() as u8", "u8::try_from(value).ok()", "value as u8"], "u8::try_from(value).ok()", "rust-bindings-types"), new("exhaustive", "A `match` on a u16 status code covers 100..=599 and has no other arm. What does the compiler do?", ["Refuses to compile, because values outside 100..=599 are not handled", "Compiles, and panics at runtime for other values", "Compiles, and silently skips the match for other values"], "Refuses to compile, because values outside 100..=599 are not handled", "rust-control-flow"), new("utf8", "What does `\"héllo\".len()` return, and why?", ["5, because len counts characters", "It does not compile, because strings with accents need a special type", "6, because len counts UTF-8 bytes and é takes two"], "6, because len counts UTF-8 bytes and é takes two", "rust-collections-strings"), new("order", "A test prints a HashMap's word counts and compares the output to a fixed string. Why might it fail on another run?", ["HashMap drops duplicate counts when printing", "HashMap iteration order is unspecified and changes between runs", "HashMap sorts keys by insertion time, which differs between machines"], "HashMap iteration order is unspecified and changes between runs", "rust-collections-strings"), new("question", "Inside a function returning `Result<u16, String>`, what does `raw.parse::<u16>().map_err(|e| e.to_string())?` do when the text is not a number?", ["Returns the Err from the function immediately", "Panics with the parse error", "Substitutes 0 and continues"], "Returns the Err from the function immediately", "rust-option-result"), new("absence", "A lookup by user ID may legitimately find nobody. Which return type says that most honestly?", ["User, returning an empty User when nothing is found", "Result<User, ()> where Err means a crash", "Option<User>"], "Option<User>", "rust-option-result"), new("test", "Your leap-year function uses only `year % 4 == 0`. Which test would expose the bug?", ["Asserting that 2023 is not a leap year", "Asserting that 1900 is not a leap year", "Asserting that 2024 is a leap year"], "Asserting that 1900 is not a leap year", "rust-debugging-tests"), new("private", "Outside its module, code writes `item.quantity = 99;` where the `quantity` field has no `pub`. What happens?", ["Compilation fails with a private-field error", "It compiles, because the struct itself is pub", "It compiles in debug builds but not release builds"], "Compilation fails with a private-field error", "rust-crates-modules"), new("lock", "Why commit Cargo.lock for a program?", ["It speeds up compilation by storing compiled code", "It stops anyone from adding new dependencies", "It records the exact dependency versions you tested, so every build uses them"], "It records the exact dependency versions you tested, so every build uses them", "rust-cargo-quality"), new("release", "Why measure performance only with `cargo build --release`?", ["Benchmarks cannot run in debug builds", "Debug builds skip most optimization and keep extra checks, so they measure a different program", "Release builds have network access and debug builds do not"], "Debug builds skip most optimization and keep extra checks, so they measure a different program", "rust-cargo-quality")]),
        new("csharp-dotnet", "foundations", "C# foundations checkpoint", [new("value", "Which type fits a whole-number count?", ["int", "string", "bool"], "int", "foundations-values"), new("condition", "What does an if statement require?", ["A boolean condition", "A database", "A loop"], "A boolean condition", "foundations-making-decisions"), new("record", "What do records express well?", ["Value-based data", "Hidden global state", "Unbounded inheritance"], "Value-based data", "modern-csharp-records")]),
        new("git-cli", "git-foundations-1", "Git CLI checkpoint", [
            new("status", "Which command is the safest first check when you are unsure what Git state you are in?", ["git status", "git push --force", "git clean -fd"], "git status", "git-init-status"),
            new("stage", "What does git add do?", ["Copies selected working-tree content into the staging area", "Publishes commits to a remote", "Deletes untracked files"], "Copies selected working-tree content into the staging area", "git-stage-files"),
            new("branch", "What is a Git branch?", ["A movable name pointing at a commit", "A duplicate copy of every repository file", "A remote server"], "A movable name pointing at a commit", "git-switch-branch"),
            new("fetch", "What does git fetch origin do to your checked-out main branch by itself?", ["Nothing; it updates remote-tracking refs without merging", "It always fast-forwards main", "It deletes local commits"], "Nothing; it updates remote-tracking refs without merging", "git-fetch-remote"),
            new("rebase", "When is rebasing usually safest?", ["On private or coordinated unpublished work", "On shared commits without telling collaborators", "Whenever a merge conflict exists"], "On private or coordinated unpublished work", "git-rebase-feature"),
            new("revert", "What is the usual safe way to undo a bad commit that has already been shared?", ["Create a new revert commit", "Reset --hard and force-push immediately", "Delete the .git directory"], "Create a new revert commit", "git-revert-push")
        ]),
        new("web-development-basics", "web-basics-checkpoint", "Web development basics checkpoint", [
            new("browser", "Where does client-side JavaScript normally execute?", ["In the browser", "Inside the database", "Inside a switch"], "In the browser", "web-basics-browser-server"),
            new("html", "Which element represents an in-page action?", ["button", "div", "span"], "button", "web-basics-html"),
            new("css", "Which CSS system is well suited to rows and columns?", ["Grid", "font-weight", "box-shadow"], "Grid", "web-basics-css-box-layout"),
            new("js", "Which array method keeps only matching entries?", ["filter", "push", "await"], "filter", "web-basics-javascript"),
            new("ts", "What type best constrains a status to known string values?", ["A string-literal union", "string", "any"], "A string-literal union", "web-basics-typescript"),
            new("http", "Which status family represents server errors?", ["5xx", "2xx", "3xx"], "5xx", "web-basics-http-json"),
            new("fetch", "What should you inspect before treating fetch JSON as successful data?", ["response.ok or status", "Nothing", "Only content length"], "response.ok or status", "web-basics-fetch-async"),
            new("debug", "Which DevTools panel shows HTTP requests and responses?", ["Network", "Elements", "Fonts"], "Network", "web-basics-devtools")
        ]),
        new("sqlite", "sqlite-foundations", "SQLite foundations checkpoint", [
            new("embedded", "What makes SQLite different from a typical client/server database?", ["It runs embedded in the application process", "It requires a separate database server", "It can only run in memory"], "It runs embedded in the application process", "sqlite-what-it-is"),
            new("types", "What do declared types provide in a normal SQLite table?", ["Type affinity", "No behavior at all", "A universal hard type system identical to every SQL engine"], "Type affinity", "sqlite-storage-types-affinity"),
            new("null", "How do you test for a missing SQL value?", ["IS NULL", "= NULL", "= ''"], "IS NULL", "sqlite-null-expressions-functions"),
            new("tx", "Why put related writes in one transaction?", ["So they commit or roll back as one unit", "To make SELECT syntax shorter", "To disable constraints"], "So they commit or roll back as one unit", "sqlite-transactions"),
            new("fk", "What must be true for SQLite foreign-key constraints to protect relationships?", ["Foreign-key enforcement must be enabled on the connection", "Every key must use AUTOINCREMENT", "The database must run on a server"], "Foreign-key enforcement must be enabled on the connection", "sqlite-foreign-keys"),
            new("index", "What is the strongest reason to add an index?", ["A real query pattern benefits from it", "Every column should always have one", "The table has more than ten rows"], "A real query pattern benefits from it", "sqlite-indexes"),
            new("wal", "What remains true in WAL mode?", ["Database writes are still serialized", "Only one reader can exist", "A TCP server is required"], "Database writes are still serialized", "sqlite-wal-locking"),
            new("params", "How should application values be placed into SQL statements?", ["Bound parameters", "String concatenation", "HTML encoding"], "Bound parameters", "sqlite-parameters")
        ]),
        new("linux-cli-bash-vim", "linux-cli-foundations", "Linux CLI / Bash / Vim checkpoint", [
            new("shell", "What does Bash do before launching most commands?", ["Parses expansions, quoting, pipes, and redirects", "Draws terminal pixels", "Schedules kernel threads"], "Parses expansions, quoting, pipes, and redirects", "linux-shell-terminal"),
            new("paths", "What does .. mean in a relative path?", ["The parent directory", "The home directory", "The filesystem root"], "The parent directory", "linux-paths-navigation"),
            new("redirect", "What does >> do?", ["Append stdout to a file", "Replace stdin", "Send SIGKILL"], "Append stdout to a file", "linux-pipes-redirection"),
            new("quote", "How should a variable containing a filename usually be expanded?", ["\"$file\"", "$file unquoted", "eval $file"], "\"$file\"", "linux-expansion-quoting"),
            new("perm", "What does chmod 750 grant to others?", ["No permissions", "Read-only", "Read and execute"], "No permissions", "linux-permissions"),
            new("args", "Which Bash expansion forwards all original arguments while preserving their boundaries?", ["\"$@\"", "$*", "$(args)"], "\"$@\"", "bash-variables-arguments"),
            new("vim", "Which key returns Vim from Insert mode to Normal mode?", ["Esc", "Tab", "Ctrl-Z"], "Esc", "vim-modes-save-quit"),
            new("vim-edit", "What does ciw mean in Vim Normal mode?", ["Change inner word", "Copy inside window", "Close current window"], "Change inner word", "vim-motions-operators")
        ]),
        new("react-lite", "react-lite-foundations", "React Lite foundations checkpoint", [
            new("jsx", "What does JSX let a component describe?", ["The UI tree it wants React to render", "A database schema", "A DNS zone"], "The UI tree it wants React to render", "react-lite-jsx-rendering"),
            new("props", "What are props?", ["Inputs passed to a component", "Global mutable variables", "Database rows only"], "Inputs passed to a component", "react-lite-components-props"),
            new("state", "What is useState for?", ["Interactive values that should trigger rendering", "Every calculated value", "Server authorization"], "Interactive values that should trigger rendering", "react-lite-state"),
            new("derived", "If activeCount can be calculated from users, what should you usually do?", ["Derive it from users", "Duplicate it in state", "Store it in localStorage"], "Derive it from users", "react-lite-conditionals-derived-data"),
            new("key", "What is the best key for changing user rows?", ["user.id", "array index", "Math.random()"], "user.id", "react-lite-lists-keys"),
            new("route", "What does React Router connect to page components?", ["URLs", "CSS variables", "Database indexes"], "URLs", "react-lite-router-pages"),
            new("api", "Why use a dummy API wrapper?", ["So components depend on an API-shaped boundary that can later use real HTTP", "To avoid async code entirely", "To store CSS"], "So components depend on an API-shaped boundary that can later use real HTTP", "react-lite-dummy-api-read"),
            new("write", "Where should generated IDs for fake created users live?", ["Inside the dummy service layer", "Inside every button", "Inside Tailwind config"], "Inside the dummy service layer", "react-lite-dummy-api-write")
        ]),
        new("react-enterprise", "react-rendering-model", "React rendering and hooks checkpoint", [
            new("purity", "Why must a React component remain pure during render?", ["React may render, restart, or discard work before commit", "JSX cannot call functions", "The DOM cannot contain state"], "React may render, restart, or discard work before commit", "react-jsx-purity"),
            new("state", "When the next state depends on the previous state, which update is safest?", ["A functional state updater", "Mutating the current state variable", "Reading the DOM directly"], "A functional state updater", "react-state-snapshots-batching"),
            new("effect", "What is useEffect primarily for?", ["Synchronizing React with an external system", "Deriving every computed value", "Replacing event handlers"], "Synchronizing React with an external system", "react-effects-synchronization"),
            new("context", "What is a good use for Context?", ["A focused ambient dependency such as current tenant", "Every server response in the application", "Every local input value"], "A focused ambient dependency such as current tenant", "react-context-provider-boundaries"),
            new("transition", "What should remain urgent when filtering a large result grid?", ["The controlled input value", "Every expensive derived result render", "A background report refresh"], "The controlled input value", "react-transitions-deferred"),
            new("external", "Which hook is designed for subscribing to an external store?", ["useSyncExternalStore", "useRef", "useInsertionEffect"], "useSyncExternalStore", "react-external-store-custom-hooks"),
            new("router", "Which React Router primitive owns page-entry reads in Data Mode?", ["A route loader", "A component mount Effect by default", "A CSS module"], "A route loader", "react-router-loaders-params-search"),
            new("state-owner", "Where should a shareable filter that must survive refresh normally live?", ["URL search params", "Only local component state", "A global store by default"], "URL search params", "react-state-ownership-server-client")
        ])
    ];
    public static ModuleAssessment? Get(string courseId, string moduleId) => All.SingleOrDefault(item => item.CourseId == courseId && item.ModuleId == moduleId);
}
