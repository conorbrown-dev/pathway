static partial class Curriculum
{
    // Authored replacements for Rust lessons that were generated from one-line templates.
    // These are methods rather than static fields so they never depend on partial-class static
    // initialization order. Each code exercise has a matching fixture in
    // sandbox-broker/src/evaluator.mjs that compiles and runs the learner's code.
    private static Lesson WithRustDepth(Lesson lesson) => lesson.Slug switch
    {
        "rust-structs-enums" => RustStructsEnums(lesson),
        "rust-traits-generics" => RustTraitsGenerics(lesson),
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
}
