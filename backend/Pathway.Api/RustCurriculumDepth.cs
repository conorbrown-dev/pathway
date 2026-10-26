static partial class Curriculum
{
    // Authored replacements for Rust lessons that were generated from one-line templates.
    // These are methods rather than static fields so they never depend on partial-class static
    // initialization order. Each code exercise has a matching fixture in
    // sandbox-broker/src/evaluator.mjs that compiles and runs the learner's code.
    private static Lesson WithRustDepth(Lesson lesson) => lesson.Slug switch
    {
        "rust-hello-functions" => RustHelloFunctions(lesson),
        "rust-bindings-types" => RustBindingsTypes(lesson),
        "rust-control-flow" => RustControlFlow(lesson),
        "rust-collections-strings" => RustCollectionsStrings(lesson),
        "rust-option-result" => RustOptionResult(lesson),
        "rust-debugging-tests" => RustDebuggingTests(lesson),
        "rust-crates-modules" => RustCratesModules(lesson),
        "rust-cargo-quality" => RustCargoQuality(lesson),
        "rust-structs-enums" => RustStructsEnums(lesson),
        "rust-traits-generics" => RustTraitsGenerics(lesson),
        "rust-errors-panics" => RustErrorsPanics(lesson),
        "rust-iterators-closures" => RustIteratorsClosures(lesson),
        "rust-smart-pointers" => RustSmartPointers(lesson),
        "rust-concurrency-send-sync" => RustConcurrencySendSync(lesson),
        "rust-async-tokio" => RustAsyncTokio(lesson),
        _ => lesson
    };

    private static Lesson RustStructsEnums(Lesson lesson) => lesson with
    {
        Subtitle = "Make invalid states impossible to represent, then let the compiler check every case.",
        Concept = "A struct groups data that is always present together. An enum lists the only states a value can be in, each carrying its own data, and `match` makes you handle every one.",
        Body = """
            ## Why types matter here
            Picture an enrollment modelled the way many C# or Java codebases would do it: one class with flags.

            ```rust
            struct Enrollment {
                email: Option<String>,
                is_active: bool,
                is_cancelled: bool,
                completed_lessons: u32,
                cancel_reason: Option<String>,
            }
            ```

            Nothing stops `is_active` and `is_cancelled` from both being `true`, or a cancelled enrollment with no reason. Every function that touches this type has to remember the rules, and one that forgets creates a bug that compiles. Rust gives you two tools to make those states impossible instead of merely discouraged.

            ## Structs: data that belongs together
            A struct names a group of fields that always exist together.

            ```rust
            #[derive(Debug, Clone, PartialEq)]
            struct Learner {
                id: LearnerId,
                email: String,
                display_name: String,
            }

            // A tuple struct with one field is a "newtype". It costs nothing at runtime,
            // but the compiler will no longer let you pass a LessonId where a LearnerId belongs.
            #[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
            struct LearnerId(u64);
            ```

            Behaviour goes in an `impl` block. The first parameter tells you what a method does with its value:

            - `&self` reads the value (most methods).
            - `&mut self` changes it in place. The caller needs a mutable binding.
            - `self` consumes it. The caller cannot use the old value afterwards, which is ideal for state transitions.
            - No `self` at all is an *associated function*, called as `Learner::new(...)`. `new` is a convention, not a keyword.

            ```rust
            impl Learner {
                fn new(id: LearnerId, email: String) -> Self {
                    Self { id, display_name: email.clone(), email }
                }

                fn rename(&mut self, name: &str) {
                    self.display_name = name.trim().to_string();
                }
            }
            ```

            Fields are private to their module unless marked `pub`. Keeping them private and offering a constructor that validates input is how a struct protects its own rules.

            ## Enums: exactly one of several states
            A C# `enum` is a set of named integers. A Rust `enum` is a *sum type*: a value is exactly one variant, and each variant can carry different data.

            ```rust
            enum Enrollment {
                Invited { email: String },
                Active { email: String, completed: u32 },
                Cancelled { reason: String },
            }
            ```

            Now "active and cancelled at once" cannot be written, and a cancelled enrollment always has a reason. You already use enums like this: `Option<T>` is `Some(T)` or `None`, and `Result<T, E>` is `Ok(T)` or `Err(E)`.

            ## match: handle every state
            `match` compares a value against patterns and runs the first arm that fits. Patterns can pull data out of a variant.

            ```rust
            fn describe(enrollment: &Enrollment) -> String {
                match enrollment {
                    Enrollment::Invited { email } => format!("Invitation sent to {email}"),
                    Enrollment::Active { completed, .. } => format!("{completed} lessons completed"),
                    Enrollment::Cancelled { reason } => format!("Cancelled: {reason}"),
                }
            }
            ```

            Delete an arm and the compiler stops with **E0004: non-exhaustive patterns**, naming the missing variant. That error is the feature. When you later add `Paused`, the compiler lists every `match` that needs a decision. A catch-all `_ =>` arm silences that help, so avoid it on enums you own.

            Shorter forms exist when you care about one case:

            - `if let Enrollment::Active { completed, .. } = &enrollment { ... }`
            - `let Enrollment::Active { email, .. } = enrollment else { return Err(NotActive); };`
            - Guards refine an arm: `Enrollment::Active { completed, .. } if *completed >= 10 => ...`

            ## Transitions that consume the old state
            Taking `self` by value makes a state change one-way. After `accept` runs, the old `Invited` value no longer exists, so nothing can act on stale state.

            ```rust
            impl Enrollment {
                fn accept(self) -> Result<Enrollment, String> {
                    match self {
                        Enrollment::Invited { email } => Ok(Enrollment::Active { email, completed: 0 }),
                        other => Err(format!("cannot accept from {}", describe(&other))),
                    }
                }
            }
            ```

            ## What an enum costs in memory
            An enum is as large as its largest variant plus a small tag that records which variant is active. Check sizes with `std::mem::size_of::<Enrollment>()`. Rust often hides the tag in unused bit patterns, so `Option<Box<T>>` is the same size as `Box<T>`, because a `Box` is never null. If one rarely used variant is much larger than the rest, box its data so every value doesn't pay for it.

            ## Derives
            `#[derive(...)]` generates standard behaviour: `Debug` for `{:?}` output, `Clone`, `PartialEq` and `Eq` for `==`, `Hash` for use as a map key, and `Default`. Derive `Copy` only for small types whose fields are all `Copy`, like the `LearnerId` newtype above.

            ## Common mistakes
            - Boolean flags and `Option` fields that must agree with each other. That is an enum waiting to be written.
            - States stored as strings like `"active"`. A typo compiles; a misspelled variant does not.
            - Public fields that let any caller break the type's rules.
            - A `_ =>` arm on your own enum, which turns the next new variant into a silent bug.

            ## Try it locally
            1. Run `cargo new enrollment_lab`, then paste the `Enrollment` enum and `describe` into `src/main.rs`.
            2. Add a `Paused { email: String }` variant. Run `cargo check` and read each E0004 error before fixing it.
            3. Write `#[test]` functions for each transition, including the ones that must be refused.
            4. Print `std::mem::size_of::<Enrollment>()`, then box the largest variant's data and compare.
            """,
        Example = """
            enum Enrollment {
                Invited { email: String },
                Active { email: String, completed: u32 },
                Cancelled { reason: String },
            }

            fn describe(e: &Enrollment) -> String {
                match e {
                    Enrollment::Invited { email } => format!("invited {email}"),
                    Enrollment::Active { completed, .. } => format!("active {completed}"),
                    Enrollment::Cancelled { reason } => format!("cancelled {reason}"),
                }
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Model an enrollment lifecycle",
            "Define the `Enrollment` enum and three functions that move it between states. The grader compiles your code with its own `main` and checks every transition, so don't write a `main` function.",
            [
                "Define `enum Enrollment` with variants `Invited { email: String }`, `Active { email: String, completed: u32 }` and `Cancelled { reason: String }`",
                "`fn accept(enrollment: Enrollment) -> Enrollment`: an invitation becomes `Active` with `completed: 0`; any other state is returned unchanged",
                "`fn complete_lesson(enrollment: Enrollment) -> Enrollment`: an active enrollment's `completed` goes up by one; any other state is returned unchanged",
                "`fn summary(enrollment: &Enrollment) -> String` returns exactly `invited <email>`, `active <completed>` or `cancelled <reason>`",
            ],
            """
            enum Enrollment {
                // Add the three variants here.
            }

            fn accept(enrollment: Enrollment) -> Enrollment {
                todo!()
            }

            fn complete_lesson(enrollment: Enrollment) -> Enrollment {
                todo!()
            }

            fn summary(enrollment: &Enrollment) -> String {
                todo!()
            }
            """,
            null,
            [],
            "Use `match` with one arm per variant you change, and `other => other` to hand back any state you don't. Destructure fields in the pattern, for example `Enrollment::Active { email, completed } => ...`, and use `format!` to build the summary.",
            ["Accepting an invitation starts at zero lessons", "Only active enrollments can complete lessons", "Cancelled enrollments stay cancelled", "Summaries match each state exactly"]),
    };

    private static Lesson RustTraitsGenerics(Lesson lesson) => lesson with
    {
        Subtitle = "Share behaviour without inheritance, and choose between static and dynamic dispatch on purpose.",
        Concept = "A trait names a behaviour. Generics let one function work for every type that has that behaviour, checked at compile time. `dyn Trait` trades a small runtime cost for mixing different types in one collection.",
        Body = """
            ## Behaviour without inheritance
            Rust has no class inheritance. You share *data* by putting one struct inside another (composition), and you share *behaviour* through traits. A trait is close to a C# or Java interface: a named set of methods a type promises to provide.

            ## Defining and implementing a trait
            ```rust
            trait Describe {
                fn describe(&self) -> String;

                // A default method. Implementers get it for free and may override it.
                fn headline(&self) -> String {
                    format!("» {}", self.describe())
                }
            }

            struct Course {
                title: String,
                lessons: u32,
            }

            impl Describe for Course {
                fn describe(&self) -> String {
                    format!("{} ({} lessons)", self.title, self.lessons)
                }
            }
            ```

            `impl Describe for Course` is separate from the struct definition. You can implement your trait for types you didn't write (even `u32` or `Vec<T>`), and other people's traits for your types.

            ## Generic functions and trait bounds
            A generic function works for any type `T` that satisfies its *bounds*:

            ```rust
            fn print_all<T: Describe>(items: &[T]) {
                for item in items {
                    println!("{}", item.describe());
                }
            }

            // The same bound written with `where`, which reads better as bounds grow.
            fn longest_title<T>(items: &[T]) -> Option<String>
            where
                T: Describe,
            {
                items.iter().map(|item| item.describe()).max_by_key(|text| text.len())
            }
            ```

            The compiler checks the bound where the function is *called*. Pass a type that doesn't implement `Describe` and you get **E0277: the trait bound is not satisfied** at the call site, not a crash at runtime.

            ## How generics run: monomorphization
            For each concrete type you use, the compiler generates a specialised copy: one `print_all` for `Course`, another for `Lesson`. Calls are direct and can be inlined, so a generic is as fast as hand-written code. The costs are longer compile times and a larger binary when one generic is used with many types.

            ## Trait objects: `dyn Trait`
            A generic `&[T]` holds only one type at a time. To keep *different* types in one collection, use a trait object:

            ```rust
            let catalog: Vec<Box<dyn Describe>> = vec![
                Box::new(Course { title: "Rust".into(), lessons: 46 }),
                Box::new(Workshop { topic: "Ownership".into() }),
            ];

            for item in &catalog {
                println!("{}", item.headline());
            }
            ```

            A `&dyn Describe` or `Box<dyn Describe>` is a *fat pointer*: one pointer to the data and one to a table of that type's method addresses (the vtable). Each call costs one indirect jump and usually can't be inlined. A trait can only be used this way if it is *dyn-compatible*: roughly, no generic methods and no methods that return `Self`.

            **Choosing:**
            - Use generics when the type is known at compile time, or performance matters in a hot path.
            - Use `dyn Trait` when you need several types in one collection, plugins chosen at runtime, or one compiled copy of a function.
            - Coming from C# interfaces, generics are usually the right default; reach for `dyn` when you actually need the mixing.

            ## Standard traits worth knowing
            - `Debug`: developer output, `{:?}`. Usually derived.
            - `Display`: user-facing output, `{}`. Implementing it also gives you `.to_string()`.
            - `Clone` / `Copy`: explicit and implicit duplication.
            - `PartialEq`, `Eq`, `Hash`, `PartialOrd`, `Ord`: comparison, map keys and sorting.
            - `From` / `Into`: conversions. `From<MyError>` for your error type is what lets `?` convert errors automatically.
            - `Default`: a sensible empty value.
            - `Iterator`: anything that produces a sequence. Implement `next` and you get `map`, `filter`, `sum` and the rest.

            ## Generic types and associated types
            Structs can be generic too:

            ```rust
            struct Page<T> {
                items: Vec<T>,
                next_cursor: Option<String>,
            }

            impl<T> Page<T> {
                fn is_last(&self) -> bool {
                    self.next_cursor.is_none()
                }
            }
            ```

            Some traits use an *associated type* instead of a type parameter when each implementer picks exactly one, like `Iterator`'s `type Item`.

            ## The orphan rule
            You may implement a trait for a type only if your crate defines the trait or the type. This stops two libraries from giving conflicting implementations of `Display for Vec<u8>`. To add an outside trait to an outside type, wrap the type in a newtype you own.

            ## Common mistakes
            - Defaulting to `Box<dyn Trait>` everywhere, out of interface habit.
            - Writing a generic API with many bounds before there are two real callers. Start concrete; generalise when the second use arrives.
            - Putting debug detail in `Display`. Display is for the people using your program.
            - Trying to share fields through a trait. Traits describe behaviour; share data by composition.

            ## Try it locally
            1. Implement `Describe` for two structs and call `print_all` with each.
            2. Try passing a type without the trait and read the E0277 message.
            3. Put both types in a `Vec<Box<dyn Describe>>`, then add a generic method to the trait and read why the trait object stops compiling.
            4. Implement `Display` for one struct and use it with `format!` and `.to_string()`.
            """,
        Example = """
            trait Billable {
                fn monthly_cents(&self) -> u32;
            }

            struct Seat { learners: u32 }

            impl Billable for Seat {
                fn monthly_cents(&self) -> u32 {
                    self.learners * 900
                }
            }

            fn total_cents<T: Billable>(items: &[T]) -> u32 {
                items.iter().map(|item| item.monthly_cents()).sum()
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Bill two kinds of plan with one trait",
            "Define a `Billable` trait, implement it for two structs, and total them both with a generic function and with a trait object. The grader compiles your code with its own `main`, so don't write a `main` function.",
            [
                "Define `trait Billable` with `fn monthly_cents(&self) -> u32`",
                "`struct Seat { learners: u32 }` bills 900 cents per learner",
                "`struct Workspace { flat_cents: u32 }` bills its `flat_cents`",
                "`fn total_cents<T: Billable>(items: &[T]) -> u32` totals a slice of one type",
                "`fn total_mixed(items: &[Box<dyn Billable>]) -> u32` totals a slice that mixes types",
            ],
            """
            trait Billable {
                fn monthly_cents(&self) -> u32;
            }

            struct Seat {
                learners: u32,
            }

            struct Workspace {
                flat_cents: u32,
            }

            // Implement Billable for Seat and Workspace.

            fn total_cents<T: Billable>(items: &[T]) -> u32 {
                todo!()
            }

            fn total_mixed(items: &[Box<dyn Billable>]) -> u32 {
                todo!()
            }
            """,
            null,
            [],
            "Write `impl Billable for Seat { ... }` and the same for `Workspace`. Both totals can be `items.iter().map(|item| item.monthly_cents()).sum()`. The body is the same; only the parameter type decides between static and dynamic dispatch.",
            ["Each type bills its own way", "A generic total works for any Billable type", "An empty list costs nothing", "A trait-object total mixes types"]),
    };
    private static Lesson RustErrorsPanics(Lesson lesson) => lesson with
    {
        Subtitle = "Make failure part of the function's type, pass it up with `?`, and save panics for bugs.",
        Concept = "A function that can fail returns `Result<T, E>`, so callers can't ignore the failure. `?` hands an error to the caller in one character. `panic!` is for broken invariants, never for input you expected to be wrong.",
        Body = """
            ## Two kinds of failure
            Rust separates failures you expect from failures that mean the program itself is wrong.

            - **Recoverable:** a file is missing, a learner typed "ten" instead of 10, a server timed out. These are normal events. The function returns `Result<T, E>` and the caller decides what to do.
            - **Unrecoverable:** an index that "can't" be out of bounds is, or an invariant you rely on is broken. Continuing would be worse than stopping, so the program panics.

            C# and Java use exceptions for both, and a method's signature doesn't say what it can throw. In Rust the signature tells you: if a function can fail in an expected way, the failure is in its return type, and the compiler warns if you ignore it.

            ## Result in practice
            `Result` is an ordinary enum: `Ok(T)` or `Err(E)`.

            ```rust
            fn parse_age(input: &str) -> Result<u8, std::num::ParseIntError> {
                input.trim().parse::<u8>()
            }

            match parse_age("42") {
                Ok(age) => println!("age {age}"),
                Err(error) => println!("could not read an age: {error}"),
            }
            ```

            Calling a function that returns `Result` and doing nothing with it triggers the `unused_must_use` warning. The compiler is telling you a failure went unhandled.

            ## The ? operator
            Writing a `match` for every call gets long. `?` does the common case for you. If the value is `Ok`, it unwraps it and carries on; if it's `Err`, it returns that error from the current function immediately.

            ```rust
            fn read_config(path: &str) -> Result<Config, ConfigError> {
                let text = std::fs::read_to_string(path)?; // io::Error becomes ConfigError via From
                let config = parse(&text)?;
                Ok(config)
            }
            ```

            `?` only works inside a function that itself returns `Result` (or `Option`). It also converts the error type: if your error type implements `From<std::io::Error>`, `?` calls that conversion for you. That's why `From` matters for error handling.

            `main` can return a `Result` too, so `?` works there: `fn main() -> Result<(), Box<dyn std::error::Error>>`. A returned error prints its `Debug` form and exits with a non-zero code.

            ## Designing your own error type
            Library-style code should say exactly what went wrong. An enum is the natural shape: one variant per failure, each carrying the data a caller needs.

            ```rust
            use std::fmt;

            #[derive(Debug, PartialEq)]
            enum EnrollError {
                CourseFull { capacity: u32 },
                AlreadyEnrolled,
                UnknownCourse(String),
            }

            impl fmt::Display for EnrollError {
                fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
                    match self {
                        EnrollError::CourseFull { capacity } => write!(f, "the course is full ({capacity} seats)"),
                        EnrollError::AlreadyEnrolled => write!(f, "you are already enrolled"),
                        EnrollError::UnknownCourse(id) => write!(f, "no course called '{id}'"),
                    }
                }
            }

            impl std::error::Error for EnrollError {}
            ```

            - `Debug` is for developers and logs.
            - `Display` is the message a person reads, so write it as a sentence fragment without a trailing period.
            - `std::error::Error` marks the type as an error. Other code can then box it as `Box<dyn Error>` or wrap it in a larger error with `source()`.

            The `thiserror` crate generates the `Display` and `From` code from attributes, and `anyhow` gives applications one convenient error type with added context. Both are widely used. Learn the hand-written version first, so you know what they generate.

            ## Converting between errors
            A few methods on `Result` and `Option` do most of the plumbing:

            - `map_err(|e| ...)` turns one error type into another, often to add context: `.map_err(|_| DurationError::NotANumber(text.to_string()))?`.
            - `ok_or(err)` / `ok_or_else(|| err)` turn an `Option` into a `Result` when absence is an error.
            - `unwrap_or(default)` and `unwrap_or_else(|e| ...)` recover with a fallback.
            - `and_then` chains steps that can each fail.

            ## When to panic
            `unwrap()` and `expect("...")` panic when they meet an `Err` or `None`. They are fine:

            - in tests, where a panic *is* the failure report;
            - in short examples and prototypes;
            - when you can prove the failure is impossible. Then use `expect` with a message saying why, for example `.expect("regex is a valid literal")`.

            They are not fine for input from users, files, networks or anything else outside your control. A web service that panics on bad input lets one malformed request take down the worker serving it. Return an error instead, and let the HTTP layer turn it into a 400.

            By default a panic unwinds the thread's stack, running `Drop` for everything on the way. A panic inside one thread doesn't stop the whole process unless it's the main thread. Some release builds set `panic = "abort"` to stop immediately and produce smaller binaries.

            ## Common mistakes
            - `.unwrap()` on parsing or I/O in real code paths.
            - `Result<T, String>`. It works, but callers can't match on what went wrong. Use an enum.
            - Logging an error and *also* returning it, so the same failure is reported twice. Handle it or pass it up, not both.
            - Throwing away the original error with `map_err(|_| ...)` when its details would help. Keep it as a field or `source()` when it matters.
            - Returning a sentinel such as `-1` or an empty string for failure. That is what `Result` and `Option` are for.

            ## Try it locally
            1. Write `fn parse_age(input: &str) -> Result<u8, ...>` and call it with "42", "abc" and "300". Print each outcome with `match`.
            2. Write a function that reads a file and parses a number from it, using `?` for both steps. Make `main` return `Result<(), Box<dyn std::error::Error>>`.
            3. Replace one `?` with `.unwrap()`, run it on a missing file, and read the panic message and exit code.
            4. Add a test with `assert_eq!(parse_age("abc").is_err(), true)` and one asserting the exact error variant.
            """,
        Example = """
            fn parse_minutes(input: &str) -> Result<u32, DurationError> {
                let text = input.trim();
                if text.is_empty() {
                    return Err(DurationError::Empty);
                }
                let minutes: u32 = text
                    .parse()
                    .map_err(|_| DurationError::NotANumber(text.to_string()))?;
                Ok(minutes)
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Parse lesson durations without panicking",
            "Define an error type for durations and two functions that return it. Bad input must come back as an `Err`, never a panic. The grader compiles your code with its own `main`, so don't write a `main` function.",
            [
                "`#[derive(Debug, PartialEq)] enum DurationError` with variants `Empty`, `NotANumber(String)` and `TooLong(u32)`",
                "`impl std::fmt::Display for DurationError` with the messages `duration is empty`, `'<text>' is not a number of minutes` and `<n> minutes is longer than the 90-minute limit`, and `impl std::error::Error for DurationError {}`",
                "`fn parse_minutes(input: &str) -> Result<u32, DurationError>`: trim the input. Blank input is `Empty`, anything that isn't a whole number is `NotANumber` holding the trimmed text, and more than 90 is `TooLong`",
                "`fn total_minutes(inputs: &[&str]) -> Result<u32, DurationError>` adds every duration and returns the first error it meets",
            ],
            """
            use std::fmt;

            #[derive(Debug, PartialEq)]
            enum DurationError {
                // Empty, NotANumber(String), TooLong(u32)
            }

            impl fmt::Display for DurationError {
                fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
                    todo!()
                }
            }

            impl std::error::Error for DurationError {}

            fn parse_minutes(input: &str) -> Result<u32, DurationError> {
                todo!()
            }

            fn total_minutes(inputs: &[&str]) -> Result<u32, DurationError> {
                todo!()
            }
            """,
            null,
            [],
            "Parse with `text.parse::<u32>()` and turn its error into yours with `.map_err(|_| DurationError::NotANumber(text.to_string()))?`. In `total_minutes`, add up `parse_minutes(input)?` in a loop: `?` returns the first error for you. Use `write!(f, ...)` inside `Display`.",
            ["Valid durations parse, ignoring whitespace", "Each kind of bad input becomes the right error", "The total stops at the first error", "Messages match exactly"]),
    };
    private static Lesson RustIteratorsClosures(Lesson lesson) => lesson with
    {
        Subtitle = "Describe what to do with a sequence, not how to step through it, and capture context with closures.",
        Concept = "An iterator produces values one at a time. Adapters like `map` and `filter` are lazy: nothing runs until a consumer such as `collect` or `sum` pulls values through. Closures are small anonymous functions that can borrow or take the variables around them.",
        Body = """
            ## From loops to pipelines
            A `for` loop says *how* to walk a list. An iterator pipeline says *what* you want from it:

            ```rust
            // Loop: index bookkeeping and a mutable result.
            let mut names = Vec::new();
            for attempt in &attempts {
                if attempt.score >= 60 {
                    names.push(attempt.learner.clone());
                }
            }

            // Pipeline: the same result, read top to bottom.
            let names: Vec<String> = attempts
                .iter()
                .filter(|attempt| attempt.score >= 60)
                .map(|attempt| attempt.learner.clone())
                .collect();
            ```

            Both compile to similar machine code. Iterators remove off-by-one indexing and bounds checks the compiler can't otherwise prove away, and each step can be read on its own.

            ## Three ways to iterate a collection
            Which method you call decides what the loop receives, and whether the collection survives:

            - `.iter()` yields `&T`: shared borrows. The collection is untouched. This is what `for x in &items` uses.
            - `.iter_mut()` yields `&mut T`, so you can change items in place: `for x in &mut items`.
            - `.into_iter()` yields `T` and consumes the collection. This is what `for x in items` uses, and `items` can't be used afterwards.

            Most "borrowed value does not live long enough" and "use of moved value" errors around loops come down to this choice.

            ## Adapters are lazy; consumers drive them
            An *adapter* (`map`, `filter`, `enumerate`, `zip`, `take`, `skip`, `chain`, `flat_map`, `rev`) wraps an iterator and returns another iterator. On its own it does nothing:

            ```rust
            let doubled = numbers.iter().map(|n| n * 2); // nothing has run yet
            ```

            The compiler warns about this: *unused `Map` that must be used: iterators are lazy and do nothing unless consumed*. A *consumer* pulls values through the whole chain:

            - `collect()` builds a collection. The type annotation chooses which: `Vec<_>`, `HashMap<_, _>`, `String`, even `Result<Vec<_>, E>`, which stops at the first error.
            - `sum()`, `count()`, `min()`, `max()`, `max_by_key()`.
            - `any()`, `all()`, `find()`, `position()`. These stop as soon as they know the answer.
            - `fold(initial, |acc, item| ...)`, the general form every other consumer can be written with.
            - `for_each`, or simply a `for` loop.

            Laziness means a chain processes one item at a time end to end, with no intermediate `Vec`s. It also works on endless sequences: `(1..).map(|n| n * n).take_while(|n| *n < 100)`.

            ## Closures and what they capture
            A closure is an anonymous function written `|parameters| body`. Unlike a `fn`, it can use variables from the scope where it is defined:

            ```rust
            let pass_mark = 70;
            let passed = |score: u32| score >= pass_mark; // borrows pass_mark
            println!("{}", passed(85));
            ```

            The compiler picks the least powerful way to capture each variable, and that choice sets which trait the closure implements:

            - **`Fn`**: only reads what it captured. Can be called any number of times, even concurrently.
            - **`FnMut`**: changes something it captured, such as a counter. Needs a mutable binding to call.
            - **`FnOnce`**: moves a captured value out, so it can be called only once.

            Every `Fn` is also `FnMut`, and every `FnMut` is also `FnOnce`. Functions that accept closures ask for the weakest bound they need: `map` takes `FnMut`, and `thread::spawn` takes `FnOnce`.

            The `move` keyword makes a closure take ownership of what it captures instead of borrowing. You need it when the closure outlives the current scope: returning it from a function, or sending it to another thread.

            ```rust
            fn make_grader(pass_mark: u32) -> impl Fn(u32) -> bool {
                move |score| score >= pass_mark // pass_mark is copied into the closure
            }
            ```

            Without `move`, the closure would borrow `pass_mark`, a parameter that disappears when `make_grader` returns, and the compiler rejects it with E0373.

            `impl Fn(u32) -> bool` means "some closure type with this signature". Every closure has its own unnamed type, so you return it with `impl Fn` (one fixed type, no allocation) or `Box<dyn Fn(u32) -> bool>` (different closures from different branches, at the cost of a heap allocation and dynamic dispatch).

            ## Building maps with the entry API
            Grouping and keeping a best-so-far value per key come up constantly. `entry` looks a key up once and lets you insert or update:

            ```rust
            use std::collections::BTreeMap;

            let mut best: BTreeMap<String, u32> = BTreeMap::new();
            for attempt in &attempts {
                best.entry(attempt.learner.clone())
                    .and_modify(|score| *score = (*score).max(attempt.score))
                    .or_insert(attempt.score);
            }
            ```

            `BTreeMap` keeps keys sorted. `HashMap` is usually faster but iterates in an arbitrary order, which matters when output order shows up in tests or in a UI.

            ## Writing your own iterator
            Implement one method, `next`, and every adapter and consumer becomes available:

            ```rust
            struct Countdown(u32);

            impl Iterator for Countdown {
                type Item = u32;

                fn next(&mut self) -> Option<u32> {
                    if self.0 == 0 {
                        return None;
                    }
                    self.0 -= 1;
                    Some(self.0 + 1)
                }
            }

            let total: u32 = Countdown(3).sum(); // 3 + 2 + 1
            ```

            ## Common mistakes
            - Forgetting a consumer, so the chain never runs.
            - `.iter().cloned().collect()` to make a copy you then only read. Borrow instead.
            - Calling `collect()` in the middle of a chain just to call another adapter. Keep the chain lazy until the end.
            - Integer division when averaging: convert to `f64` *before* dividing.
            - Side effects inside `map`. Use `for_each` or a `for` loop when the point is the effect, not a new value.
            - Squeezing everything into one clever chain. A plain `for` loop is idiomatic Rust when it reads more clearly.

            ## Try it locally
            1. Rewrite a `for` loop from an earlier exercise as an iterator chain, then back. Compare which reads better.
            2. Create a `map` without consuming it and read the compiler's warning.
            3. Write a closure that increments a captured counter. Try calling it through an immutable binding and read the error. Then return a closure from a function without `move` and read E0373.
            4. Implement `Iterator` for a struct of your own and use `filter` and `sum` on it.
            """,
        Example = """
            let passing: Vec<&str> = attempts
                .iter()
                .filter(|attempt| attempt.score >= pass_mark)
                .map(|attempt| attempt.learner.as_str())
                .collect();

            let grader = move |score: u32| score >= pass_mark;
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Report on quiz attempts with iterators and closures",
            "Define `Attempt` and four functions that analyse a list of attempts. Iterators and closures are the natural fit, but any correct code passes. The grader compiles your code with its own `main`, so don't write a `main` function.",
            [
                "`struct Attempt { learner: String, score: u32 }`",
                "`fn passing_learners(attempts: &[Attempt], pass_mark: u32) -> Vec<&str>`: the names of attempts scoring at least `pass_mark`, in their original order",
                "`fn average_score(attempts: &[Attempt]) -> Option<f64>`: the mean score, or `None` when there are no attempts",
                "`fn best_by_learner(attempts: &[Attempt]) -> std::collections::BTreeMap<String, u32>`: each learner's highest score",
                "`fn make_grader(pass_mark: u32) -> impl Fn(u32) -> bool`: a closure that returns true for scores at or above `pass_mark`",
            ],
            """
            use std::collections::BTreeMap;

            struct Attempt {
                learner: String,
                score: u32,
            }

            fn passing_learners(attempts: &[Attempt], pass_mark: u32) -> Vec<&str> {
                todo!()
            }

            fn average_score(attempts: &[Attempt]) -> Option<f64> {
                todo!()
            }

            fn best_by_learner(attempts: &[Attempt]) -> BTreeMap<String, u32> {
                todo!()
            }

            fn make_grader(pass_mark: u32) -> impl Fn(u32) -> bool {
                // Return a closure that compares its score with pass_mark.
                move |score| todo!()
            }
            """,
            null,
            [],
            "`attempts.iter().filter(...).map(|a| a.learner.as_str()).collect()` gives borrowed names. For the average, convert the total to `f64` before dividing. Use `entry(...).or_insert(...)` and `max` for the best scores. The grader needs `move |score| ...` so it owns `pass_mark`.",
            ["Passing names keep their order", "The average is exact, and None for no attempts", "Each learner keeps their best score", "A grader closure remembers its own pass mark"]),
    };
    private static Lesson RustSmartPointers(Lesson lesson) => lesson with
    {
        Subtitle = "Pick the smallest tool that fits: a box, shared ownership, a runtime borrow check, or a lock.",
        Concept = "`Box` puts one owned value on the heap. `Rc` and `Arc` let several owners share a value. `RefCell` and `Mutex` allow changing shared data by checking the borrow rules while the program runs instead of at compile time.",
        Body = """
            ## Why plain references aren't always enough
            Ownership and borrowing cover most code: one owner, many readers or one writer, all checked at compile time. Three situations need something more:

            1. A type that contains itself, such as a tree or a linked list, which has no fixed size.
            2. A value with several owners, where none of them is clearly in charge of freeing it.
            3. Changing data that is shared, when the compiler can't prove that only one writer exists at a time.

            Each standard smart pointer solves one of these, and each costs something. Choose the smallest that fits.

            ## Box: one owner, data on the heap
            `Box<T>` owns a `T` stored on the heap. The box itself is a single pointer: 8 bytes on a 64-bit machine. It frees the value when the box is dropped, exactly like any owned value.

            Use it when:

            - **A type is recursive.** This doesn't compile, because `Path` would contain a whole `Path`, infinitely: `enum Path { End, Step(String, Path) }`. Rust reports E0072, *recursive type has infinite size*. A `Box` breaks the loop with a pointer of known size:

            ```rust
            enum Path {
                End,
                Step(String, Box<Path>),
            }

            fn path_len(path: &Path) -> usize {
                match path {
                    Path::End => 0,
                    Path::Step(_, rest) => 1 + path_len(rest),
                }
            }
            ```

            - **You need a trait object**, `Box<dyn Trait>`, as in the traits lesson.
            - **A value is large** and you want moves to copy one pointer instead of the whole value.

            ## Rc: shared ownership on one thread
            `Rc<T>` (reference counted) lets several parts of a program own the same value. `Rc::clone(&a)` doesn't copy the data. It adds one to a counter and returns another pointer to the same allocation. When the last `Rc` is dropped, the count reaches zero and the value is freed.

            ```rust
            use std::rc::Rc;

            let course = Rc::new(String::from("Rust Systems"));
            let for_lesson = Rc::clone(&course);
            assert_eq!(Rc::strong_count(&course), 2);
            ```

            Write `Rc::clone(&x)` rather than `x.clone()`. Both work, but the first shows a reader that only a counter changes, not a deep copy.

            `Rc` gives shared *read-only* access: you can't get `&mut T` out of it, because another owner might be reading. It is also not thread-safe, and the compiler stops you sending it to another thread.

            Two `Rc`s that point at each other never reach a count of zero, so the memory leaks. Break cycles with `Weak<T>` (`Rc::downgrade`), a pointer that doesn't keep the value alive. Typically a child holds a `Weak` to its parent.

            ## RefCell: borrow checking at runtime
            `RefCell<T>` lets you change a value through a shared reference. It still enforces the rules (any number of readers *or* one writer), but it checks them while the program runs:

            - `.borrow()` returns a read guard.
            - `.borrow_mut()` returns a write guard.
            - Breaking the rules, for example calling `borrow_mut` while a `borrow` guard is still alive, panics with **RefCell already borrowed**.

            ```rust
            use std::cell::RefCell;
            use std::rc::Rc;

            let roster: Rc<RefCell<Vec<String>>> = Rc::new(RefCell::new(Vec::new()));
            let view = Rc::clone(&roster);

            roster.borrow_mut().push(String::from("ada")); // guard dropped at the end of the statement
            assert_eq!(view.borrow().len(), 1);             // the other owner sees the change
            ```

            `Rc<RefCell<T>>` is the standard single-threaded pattern for "shared and changeable". Keep each guard short-lived; holding a `borrow_mut()` guard in a variable while calling code that also borrows is the usual cause of that panic. `try_borrow_mut()` returns a `Result` instead of panicking.

            For small `Copy` values such as counters and flags, `Cell<T>` is simpler: `get()` and `set()` copy values in and out, so there are no guards and no panics.

            ## Arc and Mutex: the same ideas across threads
            Threads need the thread-safe versions:

            - `Arc<T>` is `Rc` with an atomic counter. It's slightly slower, and safe to share between threads.
            - `Mutex<T>` is the thread-safe `RefCell`. `.lock()` blocks until no other thread holds the lock, then returns a guard that unlocks when dropped.

            ```rust
            use std::sync::{Arc, Mutex};
            use std::thread;

            let counter = Arc::new(Mutex::new(0));
            let handles: Vec<_> = (0..4)
                .map(|_| {
                    let counter = Arc::clone(&counter);
                    thread::spawn(move || *counter.lock().unwrap() += 1)
                })
                .collect();
            for handle in handles {
                handle.join().unwrap();
            }
            assert_eq!(*counter.lock().unwrap(), 4);
            ```

            `lock()` returns a `Result` because a mutex becomes *poisoned* if a thread panicked while holding it. The data might be half-updated, and `unwrap` passes that panic on. `RwLock<T>` allows many readers or one writer. It helps only when reads vastly outnumber writes, so measure first. The next lesson covers threads, `Send` and `Sync` in depth.

            ## Choosing
            - One owner, with the value on the heap, a recursive type or a trait object: `Box<T>`.
            - Many owners, read-only, one thread: `Rc<T>`.
            - Many owners, changeable, one thread: `Rc<RefCell<T>>`, or `Cell<T>` for small `Copy` values.
            - Many owners, read-only, many threads: `Arc<T>`.
            - Many owners, changeable, many threads: `Arc<Mutex<T>>` or `Arc<RwLock<T>>`.
            - A back-reference that mustn't keep the value alive: `Weak<T>`.

            Each step down that list moves a guarantee from compile time to run time and adds a little cost: a counter, a check, a lock.

            ## Common mistakes
            - Reaching for `Rc<RefCell<T>>` to make borrow errors go away. Usually the design wants a clear owner and plain borrows; restructure first.
            - Holding a `RefCell` or `Mutex` guard longer than needed. That causes "already borrowed" panics, slow code under contention, and deadlocks.
            - `Arc<Mutex<T>>` where each thread could own its own data and send results back over a channel.
            - Reference cycles between `Rc`s, which leak memory silently.
            - `x.clone()` on an `Rc` when a reader will assume it's a deep copy.

            ## Try it locally
            1. Write `enum Path { End, Step(String, Path) }` without the box and read E0072. Add `Box` and implement `path_len`.
            2. Create an `Rc`, clone it twice, and print `Rc::strong_count` before and after dropping a clone.
            3. Hold a `.borrow()` guard in a variable, then call `.borrow_mut()` on the same `RefCell`. Read the panic, then fix it by ending the first borrow sooner.
            4. Write the threaded counter without `Mutex`, so `Arc<usize>` only, and read why `+= 1` won't compile.
            """,
        Example = """
            use std::cell::RefCell;
            use std::rc::Rc;

            let roster = Rc::new(RefCell::new(Vec::new()));
            let view = Rc::clone(&roster);

            roster.borrow_mut().push(String::from("ada"));
            assert_eq!(view.borrow().len(), 1);
            assert_eq!(Rc::strong_count(&roster), 2);
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Use Box, Rc with RefCell, and Arc with Mutex",
            "Implement a recursive type with `Box`, a roster that several owners share and change with `Rc<RefCell<...>>`, and a counter that threads share with `Arc<Mutex<...>>`. The grader compiles your code with its own `main`, so don't write a `main` function.",
            [
                "`enum Path { End, Step(String, Box<Path>) }` and `fn path_len(path: &Path) -> usize`, the number of `Step`s",
                "`fn new_roster() -> Rc<RefCell<Vec<String>>>` returns an empty shared roster",
                "`fn enroll(roster: &Rc<RefCell<Vec<String>>>, name: &str) -> bool` adds the name and returns `true`, or returns `false` if it's already there. Every clone of the `Rc` must see the change",
                "`fn roster_size(roster: &Rc<RefCell<Vec<String>>>) -> usize`",
                "`fn count_in_threads(threads: usize, per_thread: usize) -> usize`: start `threads` threads that each add 1 to one shared counter `per_thread` times, wait for them all, and return the total",
            ],
            """
            use std::cell::RefCell;
            use std::rc::Rc;
            use std::sync::{Arc, Mutex};
            use std::thread;

            enum Path {
                End,
                Step(String, Box<Path>),
            }

            fn path_len(path: &Path) -> usize {
                todo!()
            }

            fn new_roster() -> Rc<RefCell<Vec<String>>> {
                todo!()
            }

            fn enroll(roster: &Rc<RefCell<Vec<String>>>, name: &str) -> bool {
                todo!()
            }

            fn roster_size(roster: &Rc<RefCell<Vec<String>>>) -> usize {
                todo!()
            }

            fn count_in_threads(threads: usize, per_thread: usize) -> usize {
                todo!()
            }
            """,
            null,
            [],
            "`path_len` can recurse through the box with `match`. For the roster, change the `Vec` through `roster.borrow_mut()`, not a clone of it. For the counter, give each thread an `Arc::clone` of one `Arc<Mutex<usize>>`, `join` every handle, then read the total with `lock()`.",
            ["Recursion through a Box counts every step", "All Rc handles share one roster", "Enrolling twice is refused", "Threads add to one shared counter"]),
    };
    private static Lesson RustConcurrencySendSync(Lesson lesson) => lesson with
    {
        Subtitle = "Run work in parallel, pass results with channels, and let Send and Sync rule out data races at compile time.",
        Concept = "Threads run code at the same time. Rust's ownership rules, expressed through the `Send` and `Sync` marker traits, stop two threads from changing the same data without synchronisation. A data race is a compile error, not a heisenbug.",
        Body = """
            ## Why concurrency is hard, and what Rust changes
            A **data race** happens when two threads access the same memory at the same time, at least one of them writes, and nothing orders the accesses. The result depends on timing. It might pass every test and fail in production once a month. In most languages, avoiding data races is up to discipline and code review.

            Rust turns them into compile errors, using rules you already know: many readers *or* one writer, and nothing outliving its owner. Those rules apply across threads too. What Rust doesn't prevent are logic bugs such as deadlocks or doing steps in the wrong order. Those are still yours to design out.

            ## Spawning threads
            `std::thread::spawn` starts a thread and returns a `JoinHandle`. `join()` waits for it and gives back its return value, or an `Err` if it panicked.

            ```rust
            use std::thread;

            let handle = thread::spawn(|| (1..=100u64).sum::<u64>());
            let total = handle.join().unwrap();
            ```

            A spawned thread can outlive the function that started it, so its closure can't borrow local variables. Try it and you get **E0373: closure may outlive the current function, but it borrows `v`**. Either `move` ownership into the thread, or use scoped threads.

            ## Scoped threads: borrowing across threads safely
            `thread::scope` guarantees that every thread spawned inside it has finished before the scope returns. Because of that guarantee, those threads may borrow local data:

            ```rust
            fn parallel_sum(numbers: &[u64], workers: usize) -> u64 {
                let chunk_size = numbers.len().div_ceil(workers.max(1)).max(1);
                std::thread::scope(|scope| {
                    let handles: Vec<_> = numbers
                        .chunks(chunk_size)
                        .map(|chunk| scope.spawn(move || chunk.iter().sum::<u64>()))
                        .collect();
                    handles.into_iter().map(|handle| handle.join().unwrap()).sum()
                })
            }
            ```

            Each thread gets a borrowed slice. Nothing is copied and no `Arc` is needed. Scoped threads are often the simplest correct choice for "split this work, then combine".

            ## Channels: share by communicating
            Instead of several threads changing shared state, each thread can *send* its result to one receiver. `std::sync::mpsc::channel()` returns a `Sender` and a `Receiver` ("multi-producer, single-consumer"):

            ```rust
            use std::sync::mpsc;
            use std::thread;

            let (sender, receiver) = mpsc::channel();
            for (index, job) in jobs.into_iter().enumerate() {
                let sender = sender.clone();
                thread::spawn(move || sender.send((index, job * job)).unwrap());
            }
            drop(sender); // otherwise the loop below waits for a message that never comes

            for (index, square) in receiver {
                results[index] = square;
            }
            ```

            Two details matter here:

            - **Iterating a `Receiver` ends only when every `Sender` is dropped.** Forgetting `drop(sender)` for the original sender makes the program hang.
            - **Messages arrive in finishing order, not sending order.** Tag each message, here with its index, when order matters.

            Sending a value moves it into the channel, so the sending thread can't touch it afterwards. That's ownership preventing a race.

            ## Send and Sync
            Two *marker traits* decide what may cross thread boundaries. They have no methods; the compiler implements them automatically when every field qualifies.

            - **`Send`**: ownership of the value can be moved to another thread. Almost everything is `Send`. `Rc` is not: its counter isn't atomic, so two threads could update it at once. Try it and you get **E0277: `Rc<i32>` cannot be sent between threads safely**.
            - **`Sync`**: a shared reference `&T` can be used from several threads at once. `Cell` and `RefCell` are not `Sync`, because changing them through `&T` without locking would race.

            `thread::spawn` requires its closure to be `Send + 'static`; scoped threads that borrow `&T` require `T: Sync`. These bounds are how the standard library turns "no data races" into ordinary type checking.

            ## Atomics: lock-free counters and flags
            For a single number shared between threads, an atomic type such as `AtomicU64` or `AtomicBool` is simpler and faster than a `Mutex`:

            ```rust
            use std::sync::atomic::{AtomicU64, Ordering};

            struct HitCounter {
                hits: AtomicU64,
            }

            impl HitCounter {
                fn record(&self) {
                    self.hits.fetch_add(1, Ordering::Relaxed);
                }
            }
            ```

            Note that `record` takes `&self`, not `&mut self`. Atomics change through a shared reference safely, which is what makes `HitCounter` `Sync`.

            The `Ordering` says how this operation is ordered relative to *other* memory operations. `Relaxed` is enough for an independent counter. Use `Acquire`/`Release`, or `SeqCst` when unsure, when an atomic flag guards other data. The low-level foundations module covers ordering in depth.

            ## Choosing an approach
            - **Split and combine** (sum, map over chunks): scoped threads, each returning a value.
            - **Producers and a consumer**: channels.
            - **A shared counter or flag**: an atomic.
            - **Shared state that's more than one number**: `Arc<Mutex<T>>`, keeping each lock short.
            - **Lots of independent CPU-heavy items**: the `rayon` crate's `par_iter()` handles splitting and the thread pool for you.
            - **Waiting on many network calls at once**: async, which is the next lesson. Threads suit CPU-bound work.

            ## Common mistakes
            - Not dropping the original `Sender`, so the receiving loop never ends.
            - Assuming messages or thread results arrive in the order you started them.
            - Spawning one OS thread per tiny task. Threads cost memory and scheduling time, so batch the work.
            - Holding a `Mutex` guard while sending on a channel or doing slow work, which serialises your "parallel" code.
            - `unwrap()` on `join()` hiding which worker panicked and why. In real code, handle the `Err` and log it.
            - Treating the absence of a data race as a correct design. Deadlocks and ordering bugs still compile.

            ## Try it locally
            1. Spawn a thread whose closure borrows a local `Vec` and read E0373. Fix it once with `move` and once with `thread::scope`.
            2. Move an `Rc` into a thread and read the `Send` error. Swap in `Arc` and see it compile.
            3. Comment out `drop(sender)` in a channel example and watch the program hang. Use Ctrl+C to stop it.
            4. Count to a million from 8 threads with `AtomicU64`, then with `Arc<Mutex<u64>>`. Time both with `std::time::Instant`.
            """,
        Example = """
            let (sender, receiver) = std::sync::mpsc::channel();
            for (index, job) in jobs.into_iter().enumerate() {
                let sender = sender.clone();
                std::thread::spawn(move || sender.send((index, job * job)).unwrap());
            }
            drop(sender);

            for (index, square) in receiver {
                results[index] = square;
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Split work across threads, collect it in order, and count safely",
            "Implement a parallel sum with scoped threads, a channel-based job runner that keeps results in job order, and a counter that many threads share. The grader compiles your code with its own `main`, so don't write a `main` function.",
            [
                "`fn parallel_sum(numbers: &[u64], workers: usize) -> u64`: split the slice between up to `workers` threads (at least one) and add up their partial sums. Every number must be included, and an empty slice sums to 0",
                "`fn squares_in_order(jobs: Vec<u64>) -> Vec<u64>`: square each job on its own thread and return the squares in the original job order",
                "`struct HitCounter` with `fn new() -> Self`, `fn record(&self)` and `fn total(&self) -> u64`. `record` takes `&self` and must be safe to call from many threads at once",
            ],
            """
            use std::sync::atomic::{AtomicU64, Ordering};
            use std::sync::mpsc;
            use std::thread;

            fn parallel_sum(numbers: &[u64], workers: usize) -> u64 {
                todo!()
            }

            fn squares_in_order(jobs: Vec<u64>) -> Vec<u64> {
                todo!()
            }

            struct HitCounter {
                hits: AtomicU64,
            }

            impl HitCounter {
                fn new() -> Self {
                    todo!()
                }

                fn record(&self) {
                    todo!()
                }

                fn total(&self) -> u64 {
                    todo!()
                }
            }
            """,
            null,
            [],
            "Use `thread::scope` with `numbers.chunks(size)`, where `size` is the length divided by the workers and rounded up. For the jobs, send `(index, square)` through a channel, drop the original sender, and place each result at its index. `HitCounter` can use `fetch_add(1, Ordering::Relaxed)` and `load`.",
            ["The parallel sum includes every number", "Results keep their job order", "A shared counter counts every hit", "The counter is Send and Sync"]),
    };
    private static Lesson RustAsyncTokio(Lesson lesson) => lesson with
    {
        Subtitle = "Overlap waiting, not computing: futures, the Tokio runtime, timeouts, and limits on concurrency.",
        Concept = "An `async fn` returns a future, a value describing work that hasn't run yet. A runtime such as Tokio polls futures and switches between them whenever one is waiting, so a few threads can serve thousands of slow network calls. Async helps with waiting (I/O), not with CPU-heavy work.",
        Body = """
            ## The problem async solves
            A web service spends most of its time *waiting*: for the database, for another API, for the client to send more bytes. With one thread per request, 10,000 slow clients means 10,000 threads, each reserving its own stack and costing a context switch. Async lets one thread work on many tasks: when a task has to wait, it steps aside and the thread runs another.

            The concurrency lesson used threads to do *more computing at once*. Async is for *waiting on more things at once*. If the work is pure calculation, threads (or `rayon`) are still the right tool.

            ## Futures are lazy
            Calling an `async fn` runs none of its body. It returns a **future**, and nothing happens until something `.await`s it:

            ```rust
            async fn fetch_score(learner: &str) -> u32 {
                // imagine a network call here
                42
            }

            let future = fetch_score("ada"); // nothing has run
            let score = future.await;        // now it runs, pausing whenever it waits
            ```

            `.await` can only be used inside `async` code. When the awaited work isn't ready, for example the socket has no data yet, the current task pauses at that point and the runtime runs other tasks. When the data arrives, the task resumes exactly where it stopped.

            Like iterator adapters, an unawaited future does nothing, and the compiler warns that futures *do nothing unless you `.await` or poll them*.

            ## The runtime: Tokio
            Rust's standard library defines `Future`, but not what runs futures. That is a *runtime*, and Tokio is the most widely used one. `#[tokio::main]` turns `main` into an async function and starts the runtime:

            ```rust
            #[tokio::main]
            async fn main() {
                let score = fetch_score("ada").await;
                println!("{score}");
            }
            ```

            By default Tokio runs a pool of worker threads, one per CPU core. `#[tokio::main(flavor = "current_thread")]` runs everything on one thread, which is useful for tests and small tools.

            ## Doing several things at once
            Awaiting one call after another is sequential, even inside `async`. Ten 100 ms calls awaited in a loop take a full second. To overlap them, start them all before waiting for any:

            ```rust
            // tokio::spawn starts a task immediately and returns a JoinHandle.
            let handles: Vec<_> = numbers
                .into_iter()
                .map(|n| tokio::spawn(slow_square(n)))
                .collect();

            let mut results = Vec::new();
            for handle in handles {
                results.push(handle.await.unwrap()); // already running; this just collects
            }
            ```

            Tools for combining work:

            - `tokio::spawn(future)` runs a future as an independent task. Like `thread::spawn`, the future must be `Send + 'static`, so `move` what it needs.
            - `tokio::task::JoinSet` holds a group of spawned tasks and returns each result as it finishes.
            - `tokio::join!(a, b)` awaits several futures concurrently on the current task and returns all their results.
            - `tokio::select!` waits for whichever of several futures finishes first and cancels the others.

            ## Timeouts and cancellation
            Every call to something outside your process needs a time limit. Otherwise one slow dependency can hold requests open until resources run out.

            ```rust
            use std::time::Duration;

            match tokio::time::timeout(Duration::from_secs(2), fetch_score("ada")).await {
                Ok(score) => println!("{score}"),
                Err(_) => println!("timed out"),
            }
            ```

            When a timeout fires, or `select!` picks another branch, the losing future is **dropped**, and that *is* cancellation. Its code simply stops at its last `.await` and its `Drop` cleanup runs. Design async functions so that stopping at any `.await` leaves things consistent: don't leave half-written state between two awaits. Cleanup that must always happen, such as releasing a slot or decrementing a counter, belongs in a guard value's `Drop`, not on the line after an `.await` that might never resume. The grader for this lesson's exercise originally had exactly that bug: a cancelled call never decremented its in-flight counter.

            ## Limiting concurrency
            Spawning a task per item is easy, which makes it easy to send 10,000 simultaneous requests to a database that allows 50 connections. Bound how much runs at once with a `Semaphore`:

            ```rust
            use std::sync::Arc;
            use tokio::sync::Semaphore;

            let permits = Arc::new(Semaphore::new(3));
            for n in numbers {
                let permits = Arc::clone(&permits);
                tokio::spawn(async move {
                    let _permit = permits.acquire_owned().await.unwrap(); // waits while 3 are running
                    slow_square(n).await                                  // permit released when dropped
                });
            }
            ```

            The next lesson builds on this with backpressure and task ownership.

            ## Don't block the runtime
            A Tokio worker thread runs many tasks by switching at `.await` points. Code that blocks *without* awaiting freezes every task on that thread. That includes `std::thread::sleep`, a slow loop of pure computation, synchronous file or network calls, or holding a `std::sync::Mutex` across an `.await`.

            - Use the async versions: `tokio::time::sleep`, `tokio::fs`, async database drivers.
            - Move blocking or CPU-heavy work off the runtime with `tokio::task::spawn_blocking(|| ...)`.
            - Use `tokio::sync::Mutex` only when a lock must be held across an `.await`. Otherwise a short `std::sync::Mutex` section is fine and faster.

            ## Common mistakes
            - Awaiting in a loop and expecting it to be concurrent.
            - Spawning without a limit, and overwhelming a database or API.
            - No timeout on outbound calls.
            - `std::thread::sleep` or heavy computation inside an async task.
            - Ignoring `JoinHandle` results, so a panicking task fails silently.
            - Making everything `async` "for performance". Async adds complexity and pays off for I/O-bound work.

            ## Try it locally
            1. Run `cargo new async_lab`, then `cargo add tokio --features full`.
            2. Write `async fn slow_square(n: u64) -> u64` that sleeps 100 ms with `tokio::time::sleep`. Time awaiting ten of them in a loop, then spawning all ten first.
            3. Replace `tokio::time::sleep` with `std::thread::sleep` under `#[tokio::main(flavor = "current_thread")]` and watch the concurrency disappear.
            4. Wrap a call in `tokio::time::timeout` with a limit shorter than its sleep, and handle the `Err`.
            5. Add a `Semaphore` limiting the spawned calls to 3, and print how many run at once.
            """,
        Example = """
            let handles: Vec<_> = numbers
                .into_iter()
                .map(|n| tokio::spawn(slow_square(n)))
                .collect();

            let mut squares = Vec::new();
            for handle in handles {
                squares.push(handle.await.unwrap());
            }

            let limited = tokio::time::timeout(Duration::from_millis(500), slow_square(7)).await;
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Overlap slow calls, time them out, and cap concurrency",
            "The grader provides `async fn slow_square(number: u64) -> u64`, which takes 100 ms like a remote call. Don't define it yourself. Write three async functions that use it, with Tokio. The grader compiles your code with its own `#[tokio::main]`, so don't write a `main` function.",
            [
                "`async fn squares_concurrently(numbers: Vec<u64>) -> Vec<u64>`: call `slow_square` for every number at the same time and return the results in input order. Ten numbers should take about 100 ms, not 1 second",
                "`async fn with_timeout(number: u64, limit_ms: u64) -> Option<u64>`: `Some(square)` if `slow_square` finishes within `limit_ms`, otherwise `None`",
                "`async fn bounded_squares(numbers: Vec<u64>, max_in_flight: usize) -> Vec<u64>`: like `squares_concurrently`, but never more than `max_in_flight` calls running at once, with results still in input order",
            ],
            """
            use std::sync::Arc;
            use std::time::Duration;
            use tokio::sync::Semaphore;

            // The grader provides: async fn slow_square(number: u64) -> u64

            async fn squares_concurrently(numbers: Vec<u64>) -> Vec<u64> {
                todo!()
            }

            async fn with_timeout(number: u64, limit_ms: u64) -> Option<u64> {
                todo!()
            }

            async fn bounded_squares(numbers: Vec<u64>, max_in_flight: usize) -> Vec<u64> {
                todo!()
            }
            """,
            null,
            [],
            "Start every call with `tokio::spawn(slow_square(n))` before awaiting any, then await the handles in order. `tokio::time::timeout(Duration::from_millis(limit_ms), ...).await.ok()` gives the `Option`. For the cap, give each task an `Arc<Semaphore>` clone and hold `acquire_owned().await` while it calls `slow_square`.",
            ["Calls overlap instead of running one after another", "Results keep their input order", "A slow call times out", "Never more than the limit run at once"]),
    };
}
