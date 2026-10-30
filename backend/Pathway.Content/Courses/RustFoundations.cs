namespace Pathway.Content;

static partial class Curriculum
{
    // Authored replacements for the Rust foundations lessons (functions through Option/Result).
    // Like RustCurriculumDepth.cs, these are methods so they never depend on partial-class static
    // initialization order. Every code exercise has a fixture in sandbox-broker/src/evaluator.mjs
    // and reference solutions in sandbox-broker/test/rust-solutions.

    private static Lesson RustHelloFunctions(Lesson lesson) => lesson with
    {
        Title = "Functions, expressions, and formatted output",
        Subtitle = "Read and write a Rust function signature, return values without `return`, and build text with `format!`.",
        Concept = "A function's signature states exactly what goes in and what comes out. In Rust almost everything is an expression that produces a value, so a function body usually ends with the value it returns, written without a semicolon.",
        Body = """
            ## What you already have
            After the setup lesson you have a project made by `cargo new`, with a `src/main.rs` that prints a greeting. This lesson explains every piece of that file and then grows it into functions you write yourself.

            ```rust
            fn main() {
                println!("Hello, world!");
            }
            ```

            - `fn` starts a function definition.
            - `main` is the name. A binary program starts running in `main`, and the program ends when `main` returns.
            - `()` is the parameter list. `main` takes no input.
            - The `{ ... }` block is the body.
            - `println!` ends in `!`, which marks it as a **macro**, not a function. A macro is code that writes code at compile time. `println!` uses that to check, before the program ever runs, that your format string and your values match up.

            ## Parameters and return types
            Every parameter must say its type, and a function that produces a value must say the type of that value after `->`.

            ```rust
            fn rectangle_area(width: u32, height: u32) -> u32 {
                width * height
            }
            ```

            Read the signature aloud: "`rectangle_area` takes a width and a height, both unsigned 32-bit integers, and gives back an unsigned 32-bit integer." Rust never guesses parameter or return types. That is deliberate: the signature is a contract that callers can rely on without reading the body, and the compiler holds both sides to it. Inside the body, Rust *does* infer the types of local variables, so you rarely annotate those.

            `u32` means "unsigned, 32 bits": whole numbers from 0 to about 4.29 billion. The next lesson covers number types properly.

            ## Expressions and statements
            This is the idea that most surprises people coming from C#, Java or JavaScript. Look at the body of `rectangle_area` again: there is no `return` keyword, and the last line has no semicolon.

            An **expression** is code that produces a value: `width * height`, `5`, a function call, even a whole `if` or a whole block. A **statement** performs an action and produces nothing useful: `let x = 5;` is a statement. Putting a semicolon after an expression turns it into a statement and throws its value away.

            A block's value is the value of its final expression. A function body is a block, so the final expression *is* the return value. Compare:

            ```rust
            fn double(n: u32) -> u32 {
                n * 2      // value of the block: returned
            }

            fn broken_double(n: u32) -> u32 {
                n * 2;     // the semicolon discards the value; the block now produces ()
            }
            ```

            `broken_double` fails to compile with **E0308: mismatched types**, "expected `u32`, found `()`". The type `()` is called the **unit type**: the "nothing here" value that statements produce. The compiler even adds a hint: *remove this semicolon to return this value*. When you see "expected X, found `()`", look for a stray semicolon on the last line.

            `return` still exists. Use it to leave a function *early*, typically when a check fails:

            ```rust
            fn discount_percent(order_total_cents: u32) -> u32 {
                if order_total_cents < 5_000 {
                    return 0;
                }
                10
            }
            ```

            Because `if` is an expression too, you can also write the same logic as `if order_total_cents < 5_000 { 0 } else { 10 }`, which is what C# writes as `cond ? 0 : 10`. Both arms must produce the same type.

            ## Strings in signatures: &str and String
            Text appears in two forms you will see constantly.

            - `&str` (say "string slice") is a *borrowed view* of some text that lives somewhere else. String literals like `"Ada"` are `&str`. Use `&str` for parameters when the function only needs to read the text.
            - `String` is text the value *owns*, stored on the heap and able to grow. Use it when a function creates new text and hands it back.

            ```rust
            fn greet(name: &str) -> String {
                format!("Hello, {name}!")
            }
            ```

            `greet` reads the caller's name without taking it, and returns brand-new text the caller now owns. The ownership module later explains exactly why this split exists. For now the rule of thumb is: **take `&str`, return `String`**.

            ## format! and println!
            `format!` builds a `String`; `println!` prints the same kind of text plus a newline; `eprintln!` prints to the error stream instead of normal output. All three share one formatting language.

            - `{name}` inserts a variable called `name` that is in scope.
            - `{}` inserts the next argument: `format!("{} has {} lessons", course, count)`.
            - `{:?}` uses the **Debug** form, meant for developers. It works on most standard types, including lists: `println!("{:?}", vec![1, 2, 3])` prints `[1, 2, 3]`.
            - `{value:5}` pads to a width of 5. `{value:<5}` aligns left, `{value:>5}` right.
            - `{value:05}` pads a number with zeros to width 5, so `7` becomes `00007`.
            - `{value:.2}` shows two decimal places for a floating-point number.
            - `{{` and `}}` print literal braces.

            Zero-padding matters in this lesson's exercise. Money is usually stored as whole **cents** in an integer, because floating-point numbers cannot represent most decimal fractions exactly (0.1 + 0.2 is not quite 0.3). To show 305 cents as dollars, split it with integer division and remainder: `305 / 100` is `3` (division of integers drops the fraction) and `305 % 100` is `5` (`%` gives the remainder). Printed naively, that is `$3.5`, which is wrong. `{cents:02}` prints `05`.

            ## Naming and layout conventions
            Rust uses `snake_case` for functions and variables, `CamelCase` for types, and `SCREAMING_SNAKE_CASE` for constants. These are conventions rather than rules, but the compiler warns when you break them, and `cargo fmt` lays out code the standard way, so every Rust codebase reads alike.

            ## Common mistakes
            - A semicolon after the final expression, giving "expected `u32`, found `()`".
            - Forgetting the return type. A function with no `->` returns `()`, so `fn area(w: u32, h: u32) { w * h }` does not compile either.
            - Calling `println!` without `!`. Without the `!`, Rust looks for a function named `println` and doesn't find one.
            - Storing money as `f64`. Use integer cents and format at the edge.
            - Putting a variable directly inside `{}` that isn't in scope; the compiler reports **E0425: cannot find value**.

            ## Try it locally
            1. In your `hello_pathway` project, add `rectangle_area` above `main`, call it from `main` and print the result.
            2. Add a semicolon after `width * height`, run `cargo check`, and read the full E0308 message including its help line. Remove it again.
            3. Print the same number with `{}`, `{:?}`, `{:>8}` and `{:08}` to see the difference.
            4. Run `rustc --explain E0308` to read the compiler's long explanation of that error.
            """,
        Example = """
            fn greet(name: &str) -> String {
                format!("Hello, {name}!")
            }

            fn rectangle_area(width: u32, height: u32) -> u32 {
                width * height // no semicolon: this is the return value
            }

            fn main() {
                println!("{}", greet("Ada"));
                println!("area = {}", rectangle_area(3, 4));
                println!("[{:>6}] [{:06}]", 42, 42);
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Write three small functions",
            "Write the three functions below. The grader adds its own `main` that calls them, so don't write a `main` function.",
            [
                "`fn greet(name: &str) -> String` returns exactly `Hello, <name>!`, for example `Hello, Ada!`",
                "`fn rectangle_area(width: u32, height: u32) -> u32` returns width multiplied by height",
                "`fn format_price(item: &str, cents: u32) -> String` returns `<item>: $<dollars>.<cents>` with exactly two digits of cents, for example `coffee: $3.05` for 305 and `gum: $0.05` for 5",
            ],
            """
            fn greet(name: &str) -> String {
                todo!()
            }

            fn rectangle_area(width: u32, height: u32) -> u32 {
                todo!()
            }

            fn format_price(item: &str, cents: u32) -> String {
                todo!()
            }
            """,
            null,
            [],
            "End each function with an expression and no semicolon. For the price, `cents / 100` gives the dollars and `cents % 100` gives what's left; `{remainder:02}` pads it to two digits. A `$` inside a format string is just text.",
            ["The greeting matches exactly", "Area multiplies the sides", "Prices always show two digits of cents"]),
    };

    private static Lesson RustBindingsTypes(Lesson lesson) => lesson with
    {
        Title = "Bindings, number types, and overflow",
        Subtitle = "Choose the right number type, make mutation explicit, and decide what happens when a number no longer fits.",
        Concept = "Every value has a fixed-size type, and every fixed size has a limit. Rust makes you choose what happens at that limit: refuse with `checked_*`, clamp with `saturating_*`, or wrap around with `wrapping_*`. In a debug build, plain arithmetic that overflows stops the program instead of silently producing a wrong number.",
        Body = """
            ## Bindings are immutable by default
            `let` creates a **binding**: a name attached to a value.

            ```rust
            let lessons = 12;
            lessons = 13; // error[E0384]: cannot assign twice to immutable variable
            ```

            In C# or Java a local variable can change unless you mark it `readonly` or `final`. Rust flips the default: a binding never changes unless you write `mut`.

            ```rust
            let mut retries = 0;
            retries += 1;
            ```

            The point is not strictness for its own sake. When you read a function, every name without `mut` is guaranteed to keep its value, so you only have to track the few that are marked. The compiler also warns about a `mut` you never use.

            **Shadowing** is different from mutation. Writing `let` again with the same name creates a *new* binding that hides the old one, and the new one may even have a different type:

            ```rust
            let port = "8080";            // &str read from a config file
            let port: u16 = port.parse().unwrap_or(80); // now a number
            ```

            This is common when converting a value step by step, and avoids names like `port_str` and `port_num`. (`unwrap_or(80)` means "use the parsed number, or 80 if parsing failed"; the Option and Result lesson explains it.)

            Constants use `const`, need a type, and must be computable at compile time: `const MAX_LEARNERS: u32 = 10_000;`. Underscores in number literals are just for readability.

            ## Integer types
            An integer type is named by whether it can be negative and how many bits it uses:

            - Signed (can be negative): `i8`, `i16`, `i32`, `i64`, `i128`
            - Unsigned (zero and up): `u8`, `u16`, `u32`, `u64`, `u128`
            - `isize` and `usize` are as wide as a memory address on the machine: 64 bits on a typical laptop or server.

            Each extra bit doubles the number of values. A `u8` has 8 bits, so 2 to the 8th = 256 possible values: 0 through 255. An `i8` spends one bit on the sign, so it covers -128 through 127. A `u16` reaches 65,535; a `u32` about 4.29 billion; a `u64` about 18 quintillion. You can always ask: `u8::MAX`, `i32::MIN`.

            How to choose:

            - **`usize`** for anything that counts or indexes memory: lengths, positions in a list. The standard library uses it for `len()` and indexing, so using it avoids conversions.
            - **The smallest type the domain truly allows** when the size is part of the data's meaning: a byte from a file is a `u8`, a TCP port is a `u16`, an RGB colour channel is a `u8`.
            - **`i64` or `u64`** for quantities that could grow large: money in cents, timestamps, counters that run for months.
            - **`i32`** is the default Rust picks when nothing else decides. It's fine for small calculations.

            Systems programming lives on these choices. A network protocol says "this field is two bytes"; a file format says "this length is four bytes, unsigned". Picking the matching type turns that rule into something the compiler enforces.

            Literals can be written in other bases: `0xff` (hexadecimal, 255), `0b1010` (binary, 10), `0o17` (octal, 15). A suffix fixes the type: `255u8`, `1_000i64`.

            ## Floating point, bool, and char
            - `f64` is the default floating-point type (`f32` is half the size and less precise). Use it for measurements and calculations where small rounding is acceptable, never for money or exact counting.
            - `bool` is `true` or `false`. Rust never treats `0` or an empty string as false: an `if` needs an actual `bool`.
            - `char` is one **Unicode scalar value**, four bytes wide, so `'é'`, `'日'` and `'🦀'` are each a single `char`. This differs from C, where `char` is one byte. The strings lesson explains why a `String` is not simply a list of `char`s.

            ## Tuples and arrays
            A **tuple** groups a fixed number of values of possibly different types: `let point: (i32, i32) = (3, -1);`, read with `point.0` and `point.1`, or unpacked with `let (x, y) = point;`. A function can return a tuple to hand back two results.

            An **array** holds a fixed number of values of one type, stored side by side: `let header: [u8; 4] = [0x50, 0x4b, 0x03, 0x04];`. The length is part of the type, so `[u8; 4]` and `[u8; 8]` are different types. Indexing past the end (`header[10]`) stops the program with a panic rather than reading whatever memory happens to follow, which is a classic security hole in C.

            ## What happens at the limit: overflow
            Adding 1 to a `u8` holding 255 cannot produce 256; there is no bit pattern for it. This is **integer overflow**, and different languages handle it differently. C#'s default unchecked arithmetic and C both wrap around silently, so 255 + 1 becomes 0. Bugs of that kind have caused real security vulnerabilities and wrong financial results.

            Rust's rule:

            - In a **debug build** (`cargo build`, `cargo run`, `cargo test`), plain `+`, `-` and `*` **panic** on overflow: the program stops with "attempt to add with overflow". You find the bug during development.
            - In a **release build** (`cargo build --release`), the check is turned off for speed and arithmetic wraps. Code that relied on the panic to catch a problem now silently produces a wrong value.

            So when overflow is *possible*, don't leave it to plain arithmetic. Say what you want:

            - **`checked_add`** (and `checked_sub`, `checked_mul`...) returns an `Option`: `Some(result)` if it fits, `None` if not. Use it when overflow means the input is invalid. `200u8.checked_add(100)` is `None`.
            - **`saturating_add`** clamps to the type's limit. `250u8.saturating_add(10)` is `255`. Use it for counters and levels where "maximum" is a sensible answer.
            - **`wrapping_add`** wraps around on purpose. `255u8.wrapping_add(1)` is `0`. Use it when wrapping is the correct meaning, such as sequence numbers in network protocols, hash calculations and tick counters.
            - `overflowing_add` returns the wrapped value *and* a `bool` saying whether it wrapped.

            `checked_add` returning an `Option` lets you use `?` inside a function that itself returns `Option`: `total = total.checked_add(price)?;` means "add, or give up and return `None` if it doesn't fit".

            ## Converting between number types
            Rust never converts number types implicitly; `let x: u64 = some_u32;` is an error. You have two tools, and the difference matters.

            - **`as`** always succeeds, and silently changes the value if it doesn't fit. `300i32 as u8` is `44`, because only the lowest 8 bits are kept (300 is 256 + 44). `-1i32 as u8` is `255`. `3.9f64 as u32` is `3`. Use `as` only when you have already proved the value fits, or when truncation is exactly what you want.
            - **`From`** and **`TryFrom`** are checked. `u64::from(some_u32)` exists only for conversions that can never lose data. When a conversion *might* not fit, `u8::try_from(value)` returns a `Result`: `Ok(byte)` or an error. Add `.ok()` to turn that into an `Option`.

            ```rust
            let raw: i32 = 300;
            let truncated = raw as u8;              // 44, no warning
            let checked = u8::try_from(raw).ok();   // None
            ```

            Code review rule of thumb: every `as` between integer types deserves a comment explaining why the value fits.

            ## Common mistakes
            - Testing only in debug builds and shipping a release build where overflow now wraps silently.
            - Using `as` to "fix" a type mismatch, turning a compile error into a silent data bug.
            - Using `u32` for something that subtracts, like a balance or a difference between two times; `3u32 - 5` overflows. Use a signed type, or `checked_sub`.
            - Adding `mut` everywhere out of habit. Start without it and let the compiler tell you where it is needed.

            ## Try it locally
            1. In a new project, write `let x: u8 = 255; let y = x + 1;` and run `cargo run`. Read the panic message. Then run `cargo run --release` and print `y`.
            2. Print `300i32 as u8`, `-1i32 as u8` and `u8::try_from(300i32)` side by side.
            3. Try `let total: u64 = 5u32;` and read the compiler's suggestion for converting it.
            """,
        Example = """
            let mut points: u8 = 250;
            points = points.saturating_add(10);      // 255, clamped
            let wrapped = 255u8.wrapping_add(1);     // 0, on purpose
            let refused = 200u8.checked_add(100);    // None
            let truncated = 300i32 as u8;            // 44: `as` drops bits silently
            let checked = u8::try_from(300i32).ok(); // None
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Handle the limits of fixed-size numbers",
            "Write four functions that each make a deliberate choice about overflow or conversion. The grader compiles a debug build, so plain arithmetic that overflows will stop it. Don't write a `main` function.",
            [
                "`fn add_points(current: u8, earned: u8) -> u8` adds the points but never goes above 255",
                "`fn checked_total(prices: &[u32]) -> Option<u32>` adds every price, returning `None` if the total doesn't fit in a `u32` (an empty list totals `Some(0)`)",
                "`fn to_byte(value: i32) -> Option<u8>` returns `Some` only when the value is between 0 and 255; 300 and -1 are `None`, not truncated",
                "`fn next_tick(tick: u16) -> u16` adds one, and the tick after 65535 is 0",
            ],
            """
            fn add_points(current: u8, earned: u8) -> u8 {
                todo!()
            }

            fn checked_total(prices: &[u32]) -> Option<u32> {
                todo!()
            }

            fn to_byte(value: i32) -> Option<u8> {
                todo!()
            }

            fn next_tick(tick: u16) -> u16 {
                todo!()
            }
            """,
            null,
            [],
            "Each function matches one tool from the lesson: `saturating_add`, `checked_add` with `?` inside a loop over `prices`, `u8::try_from(value).ok()`, and `wrapping_add`. Avoid `as` for `to_byte`: it truncates 300 to 44.",
            ["Points clamp at the maximum", "Overflowing totals are refused", "Out-of-range values are not truncated", "The tick counter wraps on purpose"]),
    };

    private static Lesson RustControlFlow(Lesson lesson) => lesson with
    {
        Title = "Decisions, loops, and pattern matching",
        Subtitle = "Use `if`, `match`, and Rust's three loops as expressions, and let the compiler check that every case is handled.",
        Concept = "`if`, `match` and `loop` are expressions that produce values. `match` compares a value against patterns, such as ranges, alternatives, and tuples, and the compiler refuses to build a `match` that leaves any possible value unhandled.",
        Body = """
            ## if is an expression
            The condition must be a `bool`; Rust never treats a number or an empty string as false, so `if count { ... }` is an error. No parentheses are needed around the condition, but the braces are required.

            ```rust
            let label = if score >= 50 { "pass" } else { "retry" };
            ```

            Because `if` produces a value, it replaces C#'s `cond ? a : b`. Both branches must have the same type, and an `if` used for its value needs an `else`, since otherwise there is no value when the condition is false.

            ## match: one decision, every possibility
            `match` takes a value and a list of **arms**. Each arm is a *pattern*, `=>`, and an expression. Rust tries the arms from top to bottom and runs the first whose pattern fits.

            ```rust
            fn describe_retries(retries: u32) -> &'static str {
                match retries {
                    0 => "first attempt",
                    1 | 2 => "retrying",              // `|` means "either"
                    3..=9 => "struggling",            // inclusive range
                    _ => "giving up",                 // anything else
                }
            }
            ```

            The return type `&'static str` means "a string slice that lives for the whole program", which is what text literals like `"retrying"` are. You'll meet the `'static` part properly in the lifetimes lesson.

            The defining rule: **a `match` must be exhaustive.** Every possible value of the type must reach some arm, or the program does not compile (**E0004: non-exhaustive patterns**), and the error lists an example value you forgot. For a `u32` that means covering all four billion values, which is why the last arm is `_` here. For enums (next module) it means every variant.

            Patterns you will use most:

            - Literals: `0`, `'a'`, `"GET"` (string literals match `&str`).
            - Alternatives: `'a' | 'e' | 'i' | 'o' | 'u'`.
            - Ranges: `200..=299` includes both ends. Ranges in patterns must be inclusive (`..=`) or open-ended (`600..`).
            - Bindings: a plain name matches anything and gives it a name to use in the arm: `other => format!("unexpected {other}")`.
            - `name @ pattern` matches the pattern *and* binds the value: `code @ 500..=599 => format!("server error {code}")`.
            - Guards add a condition after the pattern: `n if n % 2 == 0 => "even"`. The compiler can't see inside guards, so a `match` relying on them usually still needs a final catch-all arm.
            - Tuples match several values at once, which flattens nested `if` chains:

            ```rust
            match (is_signed_in, is_admin) {
                (false, _) => "please sign in",
                (true, true) => "admin dashboard",
                (true, false) => "learner dashboard",
            }
            ```

            `_` inside a pattern means "any value, and I don't need it".

            Order matters: arms are tried top to bottom, so put specific patterns before general ones. If an arm can never be reached because an earlier arm already catches everything it would, the compiler warns **unreachable pattern**.

            ### When a catch-all hides bugs
            A catch-all `_` is necessary for numbers, but think about what it catches. An HTTP status code is a `u16`, so it can hold 0 through 65535, yet only 100 through 599 are meaningful. If the last arm is `_ => "server error"`, a corrupted value like 42 or 600 is misreported as a server error. Make the meaningful ranges explicit and give the catch-all an honest name such as `"invalid"`.

            ## if let and let else
            When you only care about one pattern, a full `match` is noisy.

            - `if let` runs a block when the pattern fits: `if let 200..=299 = status { record_success(); }`. It is most useful with `Option` and enums: `if let Some(user) = find_user(id) { ... }`.
            - `let ... else` binds the pattern or leaves the function: `let Some(user) = find_user(id) else { return Err(NotFound); };`. After that line, `user` is available for the rest of the function. This keeps the "happy path" unindented.

            ## Three loops
            **`loop`** repeats forever until you `break`. It is also an expression: `break value` makes the whole loop produce `value`, which is handy for "retry until it works" logic.

            ```rust
            let mut attempts = 0;
            let result = loop {
                attempts += 1;
                if attempts == 3 {
                    break attempts * 10;
                }
            };
            ```

            **`while`** repeats while a condition holds: `while queue_len > 0 { ... }`.

            **`for`** walks over anything iterable, and is the loop you will write most:

            - `for i in 0..5` gives 0, 1, 2, 3, 4. The `..` range excludes its end; `0..=5` includes it.
            - `for i in (0..10).rev()` counts down; `(0..10).step_by(2)` skips.
            - `for name in &names` reads each element of a list without taking the list. The ownership lessons explain why the `&` matters.

            There is no C-style `for (int i = 0; i < n; i++)`. Ranges and iterators replace it, and they cannot step past the end of a collection, which removes a whole class of off-by-one bugs.

            `continue` jumps to the next iteration. To leave an outer loop from inside an inner one, label it: `'outer: for row in grid { for cell in row { if cell == target { break 'outer; } } }`.

            ## Choosing between them
            - Several conditions on *one value* → `match`.
            - A yes/no condition → `if`.
            - Walking a range or collection → `for`.
            - Repeating until something changes, number of steps unknown → `while`, or `loop` with `break` when the exit test is in the middle or the loop must produce a value.

            ## Common mistakes
            - A wildcard arm that quietly absorbs values you never meant to accept.
            - Overlapping ranges in the wrong order, so a later, more specific arm is never reached.
            - Writing `if x = 5` instead of `if x == 5`. Rust rejects it, because assignment produces `()`, not `bool`.
            - Using `while` with a manual index to walk a list. `for item in &items` is shorter and can't go out of bounds.

            ## Try it locally
            1. Write a `match` on a `u8` with arms for `0`, `1..=9` and `10..=99`, and nothing else. Read the E0004 error: it names the values you missed.
            2. Swap two arms so a general one comes first, and read the "unreachable pattern" warning.
            3. Rewrite a nested `if` on two booleans as a `match` on a tuple.
            """,
        Example = """
            fn http_class(status: u16) -> &'static str {
                match status {
                    200..=299 => "success",
                    400..=499 => "client error",
                    500..=599 => "server error",
                    _ => "other",
                }
            }

            let mut attempts = 0;
            let succeeded_on = loop {
                attempts += 1;
                if try_connect() {
                    break attempts; // the loop's value
                }
            };
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Classify, branch, and loop",
            "Write three functions that each use a different control-flow tool. Don't write a `main` function; the grader provides one.",
            [
                "`fn http_class(status: u16) -> &'static str` returns `informational` (100–199), `success` (200–299), `redirect` (300–399), `client error` (400–499), `server error` (500–599), or `invalid` for anything else",
                "`fn fizzbuzz(n: u32) -> String` returns `FizzBuzz` when n is divisible by 3 and 5, `Fizz` for 3 only, `Buzz` for 5 only, otherwise the number as text",
                "`fn collatz_steps(start: u64) -> u32` counts the steps to reach 1, where each step halves an even number or turns an odd number n into 3n + 1 (start is at least 1; `collatz_steps(1)` is 0 and `collatz_steps(6)` is 8)",
            ],
            """
            fn http_class(status: u16) -> &'static str {
                todo!()
            }

            fn fizzbuzz(n: u32) -> String {
                todo!()
            }

            fn collatz_steps(start: u64) -> u32 {
                todo!()
            }
            """,
            null,
            [],
            "Use inclusive ranges like `100..=199` in a `match`, and end with `_ => \"invalid\"`. For FizzBuzz, `match (n % 3, n % 5)` with arms `(0, 0)`, `(0, _)`, `(_, 0)` and `_`; `n.to_string()` turns the number into text. For Collatz, a `while n != 1` loop with a mutable copy of `start` and a step counter.",
            ["Every status range is classified, and nonsense is invalid", "FizzBuzz handles all four cases", "The loop counts Collatz steps"]),
    };

    private static Lesson RustCollectionsStrings(Lesson lesson) => lesson with
    {
        Title = "Strings, UTF-8, Vec, and HashMap",
        Subtitle = "Work with text as UTF-8 rather than as an array of characters, and pick the right collection for lists and lookups.",
        Concept = "A Rust string is a sequence of UTF-8 bytes, so its length in bytes and its number of characters can differ, and you cannot index it by position. `Vec<T>` is a growable list stored contiguously; `HashMap<K, V>` maps keys to values with fast lookups but no ordering.",
        Body = """
            ## Text is bytes in UTF-8
            Computers store text as numbers. **UTF-8** is the encoding almost everything uses today: it stores each character in one to four bytes. Plain English letters take one byte each, `é` takes two, `日` takes three, and the emoji `🦀` takes four.

            Rust guarantees every `String` and `&str` holds valid UTF-8. That guarantee shapes the whole API, and explains rules that feel odd at first:

            - **`len()` returns bytes, not characters.** `"hello".len()` is 5, but `"héllo".len()` is 6, because `é` takes two bytes. To count characters, use `text.chars().count()`. That has to walk the whole string, since the only way to know where each character starts is to read the bytes in order.
            - **You cannot write `text[0]`.** If indexing returned "the byte at position 0", you'd get half a character for `"émile"`. If it returned "the character at position 0", it would secretly take time proportional to the string's length. Rust refuses to pick a misleading answer and makes you say which one you mean.

            The ways to say it:

            - `text.chars()`: each character in order (each a `char`). `text.chars().next()` is the first character, as an `Option<char>`, since the string might be empty.
            - `text.bytes()`: each raw byte (`u8`). Right for binary protocols and ASCII-only formats.
            - `text.char_indices()`: each character together with its byte position.
            - Slicing by byte range, `&text[0..3]`, works, but **panics if a boundary falls inside a character**. Only slice at positions you got from the string itself, for example from `find`.

            Even `chars()` is not the same as what a person calls a letter: some written symbols combine several `char`s, such as a flag emoji or a letter with a separate accent mark. Those are called *grapheme clusters* and need the `unicode-segmentation` crate. For most backend work, `chars()` is the right level.

            ## String and &str
            `String` owns its text on the heap and can grow: `let mut s = String::from("Hi"); s.push_str(", Ada"); s.push('!');`. `&str` borrows a view of text owned somewhere else. A `&String` automatically works where a `&str` is expected, which is why functions usually take `&str`: they then accept both.

            Everyday text methods (all return new values or views; none change the original):

            - `trim()`, `trim_start()`, `trim_end()` remove whitespace. `trim_matches(|c: char| !c.is_alphanumeric())` removes any characters you choose from both ends.
            - `split_whitespace()` splits on runs of spaces, tabs and newlines, skipping empty pieces. `split(',')` splits on an exact separator and *keeps* empty pieces. `lines()` splits on line endings.
            - `split_once('=')` returns `Some((before, after))` for the first `=`, or `None`.
            - `to_lowercase()` and `to_uppercase()` return a new `String`. They are Unicode-aware: `"ÉCOLE".to_lowercase()` is `"école"`. On a single `char`, `to_uppercase()` returns an *iterator*, because some characters uppercase to more than one (`'ß'` becomes `"SS"`).
            - `contains`, `starts_with`, `ends_with`, `find` (returns the byte position as an `Option`).
            - `parse::<u32>()` turns text into a number, returning a `Result` because the text might not be a number.

            ## Vec: a growable list
            `Vec<T>` stores values of one type next to each other in memory and grows as needed. "Next to each other" is why it is fast: the processor reads neighbouring memory very efficiently.

            ```rust
            let mut scores: Vec<u32> = Vec::new();
            scores.push(72);
            scores.push(91);
            let ready = vec![1, 2, 3]; // macro shorthand
            ```

            Reading elements has two forms, and choosing between them is a real decision:

            - `scores[5]` **panics** if index 5 doesn't exist. Use it only when you know the index is valid.
            - `scores.get(5)` returns an `Option<&u32>`: `Some(value)` or `None`. Use it when the index came from outside, such as user input or a file.

            Other common operations: `len()`, `is_empty()`, `pop()` (removes and returns the last element as an `Option`), `contains(&x)`, `sort()`, `dedup()` (removes *adjacent* duplicates, so sort first), `iter()`, and slicing with `&scores[1..3]`, which borrows a view of part of the list. A function that only reads a list should take a slice, `&[u32]`, rather than `&Vec<u32>`, because a slice accepts a `Vec`, an array, or part of either.

            Capacity: when a `Vec` runs out of room it allocates a bigger block and moves everything over. `Vec::with_capacity(n)` reserves room up front when you know roughly how many elements are coming, avoiding repeated moves.

            ## HashMap: lookups by key
            `HashMap<K, V>` stores key-value pairs and finds a key in roughly constant time, however many entries there are. It lives in `std::collections`, so it needs a `use` line.

            ```rust
            use std::collections::HashMap;

            let mut stock: HashMap<String, u32> = HashMap::new();
            stock.insert(String::from("widget"), 3);
            let widgets = stock.get("widget"); // Option<&u32>
            ```

            The **entry API** is the idiomatic way to insert-or-update in one lookup. Counting words is the classic example:

            ```rust
            let mut counts: HashMap<String, usize> = HashMap::new();
            for word in text.split_whitespace() {
                *counts.entry(word.to_lowercase()).or_insert(0) += 1;
            }
            ```

            `entry(key)` finds the slot for that key; `or_insert(0)` puts a 0 there if it was empty and, either way, hands back a mutable reference to the count; the `*` reaches through that reference so `+= 1` changes the stored number.

            **A `HashMap` has no order.** Iterating it can produce keys in any order, and the order differs between runs on purpose: Rust randomizes hashing to resist attacks where someone sends keys designed to collide. If output order matters, as in reports, API responses, and tests, either collect into a `Vec` and sort it, or use **`BTreeMap`**, which keeps keys sorted at a small cost in speed.

            Sorting by more than one rule uses `sort_by` with a comparison. To sort word counts highest first, then alphabetically:

            ```rust
            pairs.sort_by(|a, b| b.1.cmp(&a.1).then_with(|| a.0.cmp(&b.0)));
            ```

            Read it as: compare the counts in reverse (`b` before `a` gives highest first); if they're equal, compare the words normally. `|a, b| ...` is a **closure**, a small unnamed function, covered fully in the iterators lesson.

            Also in `std::collections`: `HashSet` (unique values, fast "have I seen this?"), `BTreeSet` (sorted unique values) and `VecDeque` (a queue you can push and pop at both ends).

            ## Common mistakes
            - Using `len()` as a character count, which breaks for anyone whose name isn't plain ASCII.
            - Taking the first letter with `as_bytes()[0]`, which mangles `é` into two broken characters.
            - Writing tests or API output that depend on `HashMap` iteration order. They pass on your machine and fail later.
            - Indexing a `Vec` with a number that came from user input. Use `get`.

            ## Try it locally
            1. Print `len()` and `chars().count()` for `"hello"`, `"héllo"` and `"日本語"`.
            2. Try `&"héllo"[0..2]` and read the panic message about character boundaries.
            3. Count the words in a paragraph with the entry API, print the map twice in separate runs, and notice the order change. Then sort it.
            """,
        Example = """
            use std::collections::HashMap;

            let name = "émile zola";
            assert_eq!(name.len(), 11);            // bytes
            assert_eq!(name.chars().count(), 10);  // characters

            let mut counts: HashMap<String, usize> = HashMap::new();
            for word in "the cat and the hat".split_whitespace() {
                *counts.entry(word.to_string()).or_insert(0) += 1;
            }
            let mut pairs: Vec<_> = counts.into_iter().collect();
            pairs.sort_by(|a, b| b.1.cmp(&a.1).then_with(|| a.0.cmp(&b.0)));
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Count characters and words correctly",
            "Write three text-processing functions that work for any language, not just English. Don't write a `main` function.",
            [
                "`fn char_count(text: &str) -> usize` returns the number of characters, so `\"héllo\"` is 5 and `\"日本語\"` is 3",
                "`fn initials(name: &str) -> String` returns the uppercased first character of each word, ignoring extra spaces: `\"émile zola\"` gives `ÉZ`",
                "`fn word_frequencies(text: &str) -> Vec<(String, usize)>` counts lowercased words with punctuation trimmed from both ends, skips empty words, and sorts by count (highest first), then alphabetically",
            ],
            """
            use std::collections::HashMap;

            fn char_count(text: &str) -> usize {
                todo!()
            }

            fn initials(name: &str) -> String {
                todo!()
            }

            fn word_frequencies(text: &str) -> Vec<(String, usize)> {
                todo!()
            }
            """,
            null,
            [],
            "`chars().count()` counts characters. For initials, loop over `split_whitespace()`, take `word.chars().next()`, and push every `char` from `first.to_uppercase()` onto a `String`. For frequencies, `trim_matches(|c: char| !c.is_alphanumeric())` then `to_lowercase()`, count with `entry(word).or_insert(0)`, collect the map into a `Vec`, and sort with `sort_by` as shown in the lesson.",
            ["Characters are counted, not bytes", "Initials work for accented names", "Word counts ignore case and punctuation and sort predictably"]),
    };

    private static Lesson RustOptionResult(Lesson lesson) => lesson with
    {
        Title = "Option and Result: absence and failure as values",
        Subtitle = "Replace null and surprise exceptions with types the compiler makes you handle, and pass failures upward with `?`.",
        Concept = "`Option<T>` is either `Some(value)` or `None`, for a value that may be absent. `Result<T, E>` is either `Ok(value)` or `Err(error)`, for an operation that may fail. Both are ordinary enums, so the compiler won't let you use the value inside until you've decided what happens in the other case.",
        Body = """
            ## The problem with null
            In C#, Java and JavaScript, almost any reference can be `null`, and nothing in the type tells you which ones really might be. Forget to check and you get a `NullReferenceException` at runtime, often far from where the null came from. Tony Hoare, who introduced null references, later called them his "billion-dollar mistake".

            Rust has no null. A `String` is always a string. When a value genuinely might be missing, the type says so:

            ```rust
            enum Option<T> {
                Some(T),
                None,
            }
            ```

            (You don't write this; it's built in, and `Some` and `None` are usable without any prefix.) The `<T>` means `Option` works for any type: `Option<u32>`, `Option<String>`, `Option<User>`.

            An `Option<u32>` is **not** a `u32`. You can't add 1 to it, pass it where a `u32` is expected, or print it as a number. The compiler forces you to open it first, and opening it means deciding what happens when it's `None`. The check you might forget in C# becomes one you cannot skip.

            ## Where Option appears
            - `list.get(i)`: the index might be past the end.
            - `list.first()`, `list.last()`, `iterator.next()`: there might be no elements.
            - `map.get(&key)`: the key might not be present.
            - `text.find("x")`, `text.split_once('=')`: the search might find nothing.
            - Your own functions, such as `fn find_user(id: u64) -> Option<User>`.

            ## Getting the value out
            Pick the form that says what you mean.

            ```rust
            match find_user(7) {
                Some(user) => println!("hello {}", user.name),
                None => println!("no such user"),
            }

            if let Some(user) = find_user(7) { greet(&user); }

            let name = settings.get("name").cloned().unwrap_or(String::from("guest"));
            ```

            - `unwrap_or(default)`: the value, or a fallback.
            - `unwrap_or_else(|| compute())`: the fallback is only computed when needed.
            - `unwrap_or_default()`: the type's default (0, empty string, empty list).
            - `map(|v| ...)`: transform the value if there is one, leave `None` alone. `Some(3).map(|n| n * 2)` is `Some(6)`.
            - `and_then(|v| ...)`: like `map`, for a step that itself returns an `Option`, so you don't end up with `Some(Some(x))`.
            - `ok_or(error)` / `ok_or_else(|| error)`: convert to a `Result`, giving absence an explanation.
            - `is_some()`, `is_none()`: just ask.

            And `unwrap()`: returns the value, or **panics** (stops the program) if it's `None`. `expect("message")` is the same with a better crash message. These are appropriate in tests, in quick experiments, and when an earlier line has *proved* the value is present. In code that handles real input, an `unwrap` is a crash waiting for the right input.

            ## Result: an operation that can fail
            ```rust
            enum Result<T, E> {
                Ok(T),
                Err(E),
            }
            ```

            `Result` is how Rust reports *expected* failures, the kind that happen in normal operation: a file that doesn't exist, text that isn't a number, a network timeout. `T` is the success type and `E` carries the explanation. `"42".parse::<u16>()` returns `Result<u16, ParseIntError>`, and so does `"eighty".parse::<u16>()`; the type is the same, only the variant differs.

            The `::<u16>` part is called the **turbofish**. It tells `parse` which type to produce. You can leave it off when Rust can tell from context, as in `let port: u16 = text.parse()?;`.

            Compare with exceptions. In C# a method's signature doesn't say whether or how it can fail; you find out from documentation, or in production. In Rust a function returning `Result` announces in its signature that it can fail and what the error looks like, and the caller cannot reach the value without handling `Err`. Rust also warns if you call a function returning `Result` and ignore it entirely.

            The same methods exist on `Result`: `match`, `if let Ok(v)`, `unwrap_or`, `map`, `and_then`, plus `map_err(|e| ...)`, which converts the error into a different error type, often a friendlier one:

            ```rust
            let port: u16 = raw.parse().map_err(|_| format!("invalid port: {raw}"))?;
            ```

            `.ok()` turns a `Result` into an `Option`, discarding the error. Use it only when the reason genuinely doesn't matter.

            ## The ? operator
            Most code doesn't handle an error where it happens; it passes it to the caller. Writing a `match` for every step would bury the real logic, so Rust has `?`:

            ```rust
            fn port(config: &str) -> Result<u16, String> {
                let raw = find_setting(config, "port").ok_or_else(|| String::from("missing port"))?;
                let number = raw.parse::<u16>().map_err(|_| format!("invalid port: {raw}"))?;
                Ok(number)
            }
            ```

            `expression?` means: if this is `Ok(v)`, continue with `v`; if it's `Err(e)`, **return `Err(e)` from the current function right now**. On an `Option`, `?` returns `None` early. Two rules follow:

            - `?` only works inside a function whose return type can carry that failure. Using `?` on a `Result` inside a function that returns `u16` is an error, because there's nowhere to send the `Err`.
            - The error types must line up. Above, `map_err` converts the parse error into a `String` so it matches the function's `Result<u16, String>`.

            The errors lesson that follows builds proper error *types* (enums instead of `String`) and shows how `?` converts between them automatically. A `String` error is fine while learning.

            ## Collecting results
            A common task is "parse every item, and fail if any one is bad". Iterators can collect into a `Result` directly:

            ```rust
            let numbers: Result<Vec<i32>, _> = inputs.iter().map(|s| s.trim().parse::<i32>()).collect();
            ```

            The result is `Ok(all_numbers)` if every parse succeeded, or the first `Err`, after which it stops. A plain `for` loop that returns early on the first error does exactly the same thing; use whichever reads more clearly to you.

            ## Option or Result?
            - The value may legitimately not exist, and that's not a problem: `Option`. "No user with that ID" in a lookup.
            - Something went wrong and the caller may want to know why: `Result`. "The config file has an invalid port."
            - The program's own assumptions are broken and continuing would be dangerous: `panic!`. The next lesson covers where that line sits.

            ## Common mistakes
            - `unwrap()` on anything derived from user input, files or the network.
            - Converting every `Result` to `Option` with `.ok()` and losing the reason it failed.
            - Returning `Option` when the caller needs to tell different failures apart. If there are two ways to fail, that's a `Result` with an error that says which.
            - Using a "magic" value like `-1` or `""` to mean "not found". That's null by another name.

            ## Try it locally
            1. Write `let n: u16 = "70000".parse().unwrap();`, run it, and read the panic. Then handle it with `match`.
            2. Use `?` in `main` by changing its signature to `fn main() -> Result<(), Box<dyn std::error::Error>>` and returning `Ok(())` at the end. Make the parse fail and see what's printed.
            3. Write a lookup that returns `Option`, and call it with `if let`, `unwrap_or` and `let ... else`.
            """,
        Example = """
            fn find_setting(config: &str, key: &str) -> Option<String> {
                for line in config.lines() {
                    let Some((name, value)) = line.split_once('=') else { continue };
                    if name.trim() == key {
                        return Some(value.trim().to_string());
                    }
                }
                None
            }

            fn port(config: &str) -> Result<u16, String> {
                let raw = find_setting(config, "port").ok_or_else(|| String::from("missing port"))?;
                raw.parse::<u16>().map_err(|_| format!("invalid port: {raw}"))
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Read a config file without crashing",
            "Write three functions for reading `key = value` settings. Every bad input must come back as `None` or `Err`; none may panic. Don't write a `main` function.",
            [
                "`fn find_setting(config: &str, key: &str) -> Option<String>` returns the trimmed value of the first `key = value` line whose trimmed key matches; skip lines starting with `#` and lines without `=`",
                "`fn port(config: &str) -> Result<u16, String>` returns the `port` setting as a number, `Err(\"missing port\")` if it isn't there, or `Err(\"invalid port: <value>\")` if it isn't a valid `u16`",
                "`fn parse_all(inputs: &[&str]) -> Result<Vec<i32>, String>` trims and parses every input, or returns `Err(\"bad number: <input>\")` for the first one that fails",
            ],
            """
            fn find_setting(config: &str, key: &str) -> Option<String> {
                todo!()
            }

            fn port(config: &str) -> Result<u16, String> {
                todo!()
            }

            fn parse_all(inputs: &[&str]) -> Result<Vec<i32>, String> {
                todo!()
            }
            """,
            null,
            [],
            "In `find_setting`, loop over `config.lines()`, trim each line, `continue` past comments, and use `let Some((name, value)) = line.split_once('=') else { continue };`. In `port`, chain `ok_or_else` and `?`, then `parse::<u16>()` with `map_err`. In `parse_all`, either collect into `Result<Vec<i32>, String>` or loop and `return Err(...)` at the first failure.",
            ["Settings are found, trimmed, and comments ignored", "Missing and invalid ports are errors, not crashes", "One bad number fails the whole list"]),
    };
}
