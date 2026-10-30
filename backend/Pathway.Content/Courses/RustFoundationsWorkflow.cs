namespace Pathway.Content;

static partial class Curriculum
{
    // Authored replacements for the Rust foundations workflow lessons: debugging and tests,
    // modules, and Cargo. Fixtures live in sandbox-broker/src/evaluator.mjs.

    private static Lesson RustDebuggingTests(Lesson lesson) => lesson with
    {
        Title = "Read compiler errors, debug, and write tests",
        Subtitle = "Treat the compiler, the debugger and your tests as evidence: read what they say, then prove a fix with a test that failed first.",
        Concept = "A Rust compiler error names the problem, points at the exact code, and usually suggests a fix. When the program compiles but behaves wrongly, a debugger or `dbg!` shows what it's really doing, and a test written to fail on the bug proves the fix and keeps the bug from coming back.",
        Body = """
            ## How to read a compiler error
            Beginners often experience the Rust compiler as an obstacle. Experienced Rust developers treat it as a reviewer that has already read every line. Its messages have a consistent shape, and reading them in order saves a lot of guessing:

            ```text
            error[E0382]: borrow of moved value: `name`
             --> src/main.rs:5:20
              |
            3 |     let name = String::from("Ada");
              |         ---- move occurs because `name` has type `String`, which does not implement the `Copy` trait
            4 |     let owner = name;
              |                 ---- value moved here
            5 |     println!("{}", name);
              |                    ^^^^ value borrowed here after move
              |
            help: consider cloning the value if the performance cost is acceptable
              |
            4 |     let owner = name.clone();
              |                     ++++++++
            ```

            1. **The code and headline**: `E0382`, "borrow of moved value". Every error has a code, and `rustc --explain E0382` prints a full explanation with examples.
            2. **The location**: file, line and column. Editors make this clickable.
            3. **The labelled spans**: the `^^^^` marks where the problem shows up, and the `----` lines mark *related* places, often the real cause. Here the cause is line 4, not line 5.
            4. **`help:` and `note:`**: suggested fixes, sometimes with the exact code to add (`++++`). Read them, but judge them: "clone it" compiles, but whether copying is the right design is your decision.

            **Fix the first error first.** One mistake often causes several follow-on errors, and they disappear with it. **Warnings matter too**: an "unused variable" or "unused `Result`" warning is frequently the actual bug.

            The fastest loop is `cargo check`: it runs the compiler's analysis without producing a program, so it's much quicker than `cargo build`. Editors with **rust-analyzer** run it continuously and underline problems as you type.

            Errors you'll meet constantly in the coming lessons:

            - **E0308 mismatched types**: often a stray semicolon, or a number type that needs converting.
            - **E0425 cannot find value**: a typo, or a name defined in another scope or module.
            - **E0382 use of moved value**: ownership was given away (next module).
            - **E0502 / E0499**: two borrows conflict, for example reading a list while also changing it.
            - **E0004 non-exhaustive patterns**: a `match` is missing a case.

            ## When it compiles but is wrong
            Compiling proves the program is *memory-safe and type-correct*, not that it does what you meant. For logic bugs you need to see what the program actually does.

            **`dbg!`** prints an expression, its value, and the file and line, then hands the value back unchanged, so you can wrap it around part of an expression without restructuring code:

            ```rust
            let total = dbg!(price * quantity) + shipping;
            // [src/main.rs:8:17] price * quantity = 1500
            ```

            It prints to the error stream, so it doesn't mix with real output. Remove it before committing; Clippy can be told to flag leftovers. `eprintln!` is the general-purpose way to print diagnostics.

            **Backtraces.** When a program panics, set `RUST_BACKTRACE=1` to see the chain of function calls that led there: `RUST_BACKTRACE=1 cargo run` on macOS/Linux, or `$env:RUST_BACKTRACE=1` in PowerShell before running. Read from the top down to the first frame in *your* code.

            **A debugger** stops the program at a line you choose, a **breakpoint**, and lets you inspect every variable, then step forward one line at a time. That beats adding print statements once a bug involves more than a couple of values.

            - In VS Code, install the **CodeLLDB** extension alongside rust-analyzer. A "Debug" link appears above `main` and above each test. Click in the margin to set a breakpoint.
            - RustRover has a debugger built in.
            - From a terminal, Rust installs wrappers that understand Rust types: `rust-gdb` (Linux) and `rust-lldb` (macOS). Build with `cargo build`, then run `rust-gdb target/debug/your_program`. Inside: `break src/main.rs:12`, `run`, `print total`, `next` (next line), `step` (into a function call), `bt` (backtrace), `continue`.

            Debug builds keep the information a debugger needs; release builds optimize much of it away, which is why variables can show as "optimized out" in a release binary.

            ## Tests
            A **test** is a function marked `#[test]`. `cargo test` compiles a special build of your code containing every test, runs them all, and reports which failed. A test *fails* if it panics, which is exactly what the assert macros do when something is wrong:

            - `assert!(condition)` and `assert!(condition, "message {}", detail)`.
            - `assert_eq!(left, right)` shows both values when they differ, so prefer it to `assert!(a == b)`.
            - `assert_ne!(left, right)`.

            Unit tests normally live in the same file as the code, in a module that is only compiled for testing:

            ```rust
            fn is_leap_year(year: u32) -> bool {
                (year % 4 == 0 && year % 100 != 0) || year % 400 == 0
            }

            #[cfg(test)]
            mod tests {
                use super::*;

                #[test]
                fn centuries_are_not_leap_years() {
                    assert!(!is_leap_year(1900), "1900 is divisible by 100 but not by 400");
                }
            }
            ```

            - `#[cfg(test)]` means "only include this when building tests", so test code never ships in your program.
            - `use super::*;` brings everything from the surrounding file into the test module, including private functions. Rust lets unit tests test private code directly.

            More tools:

            - `#[should_panic(expected = "divide by zero")]` for a test that passes only if the code panics with that message.
            - A test can return `Result<(), String>` and use `?` instead of `unwrap`.
            - `#[ignore]` skips a slow test unless you run `cargo test -- --ignored`.
            - `cargo test leap` runs only tests whose names contain "leap". `cargo test -- --nocapture` shows `println!` output from passing tests, which is normally hidden.

            **Integration tests** go in a `tests/` folder next to `src/`. Each file there is compiled as a separate program that can only use your library's *public* interface, the way another project would. **Documentation tests** are code examples in `///` comments; `cargo test` compiles and runs them, so your examples can't silently go out of date:

            ```rust
            /// Returns the area of a rectangle.
            ///
            /// ```
            /// assert_eq!(my_crate::area(3, 4), 12);
            /// ```
            pub fn area(width: u32, height: u32) -> u32 { width * height }
            ```

            ## What to test
            A useful test checks one behaviour and has a name that says which. Good inputs to choose:

            - A **typical** case, to show the feature works at all.
            - **Boundaries**: zero, one, empty, the maximum, the first and last valid values, and the first invalid ones.
            - **The rule's exceptions.** The leap-year rule is "every 4 years, except centuries, except every 400 years". Each "except" needs its own test, and the tests that matter are the ones *a wrong implementation would fail*. A test of 2024 passes for both the right rule and the naive `year % 4 == 0`, so it can't tell them apart.

            ## Fixing a bug with a test
            The professional habit: before changing the code, write a test that reproduces the bug and **watch it fail**. Then fix the code and watch it pass. The failing run proves the test actually detects the bug (a test that passes either way protects nothing), and the test stays in the suite so the bug can't quietly return.

            ## Common mistakes
            - Reading only the last error line, or fixing errors bottom-up.
            - Tests that run the code but assert nothing, or assert only the easy case.
            - Applying the compiler's `help` suggestion without asking whether it fits the design.
            - Leaving `dbg!` and `println!` debugging in committed code.

            ## Try it locally
            1. Run `cargo new --lib calendar`, replace `src/lib.rs` with the starter code from this exercise, and run `cargo test`. It passes even though the code is wrong.
            2. Add a test for 1900, watch it fail, read the output, then fix the function.
            3. Set a breakpoint inside `is_leap_year` and run that one test under the debugger. Inspect `year` and step through the condition.
            4. Add a doc test above the function and run `cargo test --doc`.
            """,
        Example = """
            #[cfg(test)]
            mod tests {
                use super::*;

                #[test]
                fn centuries_are_not_leap_years() {
                    assert!(!is_leap_year(1900));
                }

                #[test]
                fn every_fourth_century_is() {
                    assert!(is_leap_year(2000));
                }
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Find a bug with tests, then fix it",
            "The starter's `is_leap_year` has a bug that its one test doesn't catch. Write tests that expose it, fix the function, and add `days_in_month`. The grader runs your tests **and** its own, so all of them must pass, and it checks that you wrote at least three tests of your own.",
            [
                "Fix `fn is_leap_year(year: u32) -> bool` for the Gregorian rule: divisible by 4, except centuries, except every 400 years (1900 is not a leap year; 2000 is)",
                "Add `fn days_in_month(year: u32, month: u32) -> Option<u32>`: `Some(31)`, `Some(30)`, or `Some(28)`/`Some(29)` for February depending on the year; `None` for month 0 or above 12",
                "Keep a `#[cfg(test)] mod tests` with at least three `#[test]` functions of your own, including at least one that the original buggy function would fail",
            ],
            """
            /// Returns true for leap years in the Gregorian calendar.
            fn is_leap_year(year: u32) -> bool {
                year % 4 == 0
            }

            /// Number of days in `month` (1–12) of `year`, or None for a month that doesn't exist.
            fn days_in_month(year: u32, month: u32) -> Option<u32> {
                todo!()
            }

            #[cfg(test)]
            mod tests {
                use super::*;

                #[test]
                fn a_typical_leap_year() {
                    assert!(is_leap_year(2024));
                }
            }
            """,
            null,
            [],
            "The full rule is `(year % 4 == 0 && year % 100 != 0) || year % 400 == 0`. Test 1900 and 2000, since those are the years a wrong rule gets wrong. For `days_in_month`, `match month` with arms like `1 | 3 | 5 | 7 | 8 | 10 | 12 => Some(31)`, a guarded arm `2 if is_leap_year(year) => Some(29)`, and `_ => None`.",
            ["The leap-year rule handles centuries", "Month lengths are correct and invalid months are None", "Your own tests exist and pass"]),
    };

    private static Lesson RustCratesModules(Lesson lesson) => lesson with
    {
        Title = "Crates, modules, and visibility",
        Subtitle = "Organize code into modules, decide what each one exposes, and understand packages, crates and workspaces.",
        Concept = "A crate is one unit of compilation: a program or a library. Inside it, modules form a tree that groups code and controls visibility. Everything is private to its module unless marked `pub`, so a module can protect its own rules by exposing only a small, deliberate interface.",
        Body = """
            ## Packages, crates, and modules
            Three words, three levels:

            - A **package** is what `cargo new` creates: a folder with a `Cargo.toml`. It's the unit you build, test and publish.
            - A **crate** is one compilation unit. A package contains a **binary crate** if it has `src/main.rs` (it produces a program you can run) and a **library crate** if it has `src/lib.rs` (it produces code other crates can use). A package can have both, and extra binaries in `src/bin/`.
            - A **module** is a named section of code inside a crate. Modules nest into a tree whose root is `main.rs` or `lib.rs`.

            A common, sensible layout puts the real logic in `lib.rs` and keeps `main.rs` thin: it reads arguments, calls the library, and prints results. Integration tests and other programs can then use the library directly.

            ## Declaring modules
            A module can be written inline:

            ```rust
            mod inventory {
                pub fn restock() { /* ... */ }
            }
            ```

            In real projects each module usually gets its own file. Writing `mod inventory;` (with a semicolon, no body) in `main.rs` or `lib.rs` tells the compiler to load `src/inventory.rs`. A nested module `inventory::report` is declared inside `inventory.rs` with `mod report;` and lives at `src/inventory/report.rs`. The file system mirrors the module tree, but **files are not modules until a `mod` line declares them**. A forgotten `mod` line is why a new file can seem to be ignored.

            ## Paths
            Code refers to items by path, like a file path:

            - `crate::inventory::Item`: an absolute path from the root of the current crate.
            - `self::report::describe`: relative to the current module.
            - `super::Item`: the parent module, like `..` in a file system.
            - `std::collections::HashMap`, `serde::Deserialize`: a path into another crate, starting with that crate's name.

            `use` creates a shortcut so you don't repeat long paths: `use std::collections::HashMap;` lets you write `HashMap` afterwards. Convention: import types and traits by their name, and import functions by their *parent module* (`use std::fs;` then `fs::read_to_string(...)`), so a reader can see where a function comes from.

            ## Visibility: private by default
            Every item is **private to the module it's defined in** (and visible to that module's children) unless marked `pub`. This is the opposite of what many people expect, and it's deliberate: you choose what forms the module's public interface, and everything else can change without breaking anyone.

            - `pub`: visible wherever the parent module is visible.
            - `pub(crate)`: visible anywhere in this crate, but not to other crates. Good for internal helpers shared between modules.
            - `pub(super)`: visible to the parent module only.

            **Struct fields have their own visibility.** `pub struct Item` makes the type public, but each field is still private unless it's marked `pub` too. That is how a type protects its rules:

            ```rust
            mod inventory {
                pub struct Item {
                    pub name: String,   // anyone may read and change the name
                    quantity: u32,      // only code inside `inventory` can touch this
                }

                impl Item {
                    pub fn new(name: &str, quantity: u32) -> Item {
                        Item { name: name.to_string(), quantity }
                    }

                    pub fn quantity(&self) -> u32 {
                        self.quantity
                    }

                    pub fn remove(&mut self, amount: u32) -> Result<(), String> {
                        if amount > self.quantity {
                            return Err(format!("only {} left", self.quantity));
                        }
                        self.quantity -= amount;
                        Ok(())
                    }
                }
            }
            ```

            Outside `inventory`, the only way to change `quantity` is through `remove`, so the rule "you can't remove more than you have" holds everywhere, permanently. Writing `item.quantity = 99;` outside the module fails with **E0616: field `quantity` of struct `Item` is private**. A private field also means outside code can't build an `Item` with `Item { ... }` syntax; it must use the constructor, which can validate its input.

            The method `quantity(&self)` is a **getter**: it lets anyone *read* the value while only the module can *write* it. Rust has no special property syntax; a method named after the field is the convention.

            Enums are different: if an enum is `pub`, all its variants are public, since a variant you can't name is useless to a caller.

            ## Re-exports: a public path that differs from the internal layout
            `pub use` imports an item *and* makes it available under the new path. Libraries use it to offer a short, stable interface while organizing their internals however they like:

            ```rust
            pub use inventory::report::describe;
            // callers write `describe(&item)` or `my_crate::describe(&item)`
            ```

            If you later move `describe` into a different internal module, only the `pub use` line changes; no caller notices.

            ## Depending on other crates
            Another crate is a dependency, listed in `Cargo.toml`:

            ```toml
            [dependencies]
            serde = { version = "1", features = ["derive"] }
            ```

            Or from the terminal: `cargo add serde --features derive`. Cargo downloads it from **crates.io**, the public registry, builds it, and makes it available by name: `use serde::Deserialize;`. The next lesson explains version numbers, the lock file and features.

            ## Workspaces
            When a project grows into several related packages, such as a library, a command-line tool and a server sharing it, a **workspace** groups them. A top-level `Cargo.toml` lists the members:

            ```toml
            [workspace]
            members = ["core", "cli", "server"]
            resolver = "3"
            ```

            All members share one `Cargo.lock` (so they agree on dependency versions) and one `target/` build folder (so shared code compiles once). `cargo test` at the top runs every member's tests; `cargo run -p cli` picks one. A member depends on another by path: `core = { path = "../core" }`.

            ## Designing module boundaries
            - Group by what the code is *about* (`inventory`, `billing`), not by what kind of code it is (`models`, `utils`). A change to one feature should mostly touch one module.
            - Keep the public surface small. Every `pub` item is a promise you'll have to keep or break loudly.
            - Make invalid states unrepresentable from outside: private fields, constructors that validate, methods that maintain the rules.
            - If two modules need each other's private details, they probably belong together, or need a `pub(crate)` seam.

            ## Common mistakes
            - Creating `src/report.rs` and forgetting the `mod report;` line, then wondering why nothing in it is found.
            - Making every field `pub` to silence a privacy error, which removes the protection the module existed to give.
            - Marking a function `pub` inside a private module and expecting outside code to reach it. An item is only as reachable as the modules on its path.
            - Deep `use crate::a::b::c::d::...` chains everywhere; a few `pub use` lines usually make a cleaner interface.

            ## Try it locally
            1. Run `cargo new --lib shop` and move the `inventory` module into `src/inventory.rs`, with `pub mod inventory;` in `lib.rs`.
            2. Add `src/main.rs` that uses `shop::inventory::Item`, and try to set `quantity` directly. Read E0616.
            3. Move `describe` into `src/inventory/report.rs`, add `pub use` in `lib.rs`, and confirm `main.rs` didn't need to change.
            """,
        Example = """
            mod inventory {
                pub struct Item {
                    pub name: String,
                    quantity: u32, // private: only this module can change it
                }

                impl Item {
                    pub fn new(name: &str, quantity: u32) -> Item {
                        Item { name: name.to_string(), quantity }
                    }
                    pub fn quantity(&self) -> u32 {
                        self.quantity
                    }
                }
            }

            pub use inventory::Item;
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Protect a module's rules with visibility",
            "Build an `inventory` module whose stock level can only change through its methods. The grader calls your code from outside the module, and also tries to set the quantity directly, which must fail to compile. Don't write a `main` function.",
            [
                "A module `inventory` containing `pub struct Item` with a public `name: String` and a **private** `quantity: u32`",
                "`Item::new(name: &str, quantity: u32) -> Item`, a public getter `quantity(&self) -> u32`, and `remove(&mut self, amount: u32) -> Result<(), String>` that refuses to remove more than is in stock with `Err(\"only <n> left\")` and changes nothing",
                "A public nested module `inventory::report` with `describe(item: &Item) -> String` returning `<name> x<quantity>`, re-exported at the crate root with `pub use` so callers can write `describe(&item)`",
            ],
            """
            mod inventory {
                pub struct Item {
                    // Add the fields here.
                }

                impl Item {
                    // new, quantity, remove
                }

                // Add a public `report` module with `describe`.
            }

            // Re-export `describe` here with `pub use`.
            """,
            null,
            [],
            "Leave `pub` off the `quantity` field. Inside `report`, write `use super::Item;` to refer to the parent module's type, and call `item.quantity()` rather than reading the field. The re-export is `pub use inventory::report::describe;`.",
            ["The stock level is private", "Removing stock enforces the rule", "The public path and the re-export both work"]),
    };

    private static Lesson RustCargoQuality(Lesson lesson) => lesson with
    {
        Title = "Cargo: dependencies, profiles, features, and quality tools",
        Subtitle = "Understand what Cargo.toml and Cargo.lock promise, build debug and release profiles, use features and `cfg`, and run formatting, linting and audits.",
        Concept = "Cargo turns one manifest into a reproducible build. Version requirements say which dependency releases are acceptable; the lock file records exactly which ones were used. Profiles decide how code is compiled, and features plus `cfg` decide which code is compiled at all. `fmt`, `clippy` and `audit` keep the result consistent, idiomatic and free of known vulnerabilities.",
        Body = """
            ## Cargo.toml: the manifest
            ```toml
            [package]
            name = "learner_tool"
            version = "0.3.1"
            edition = "2024"
            rust-version = "1.85"

            [dependencies]
            serde = { version = "1.0.200", features = ["derive"] }
            anyhow = "1"

            [dev-dependencies]
            proptest = "1"
            ```

            - `edition` selects the language edition. Editions let Rust make small breaking changes every few years without breaking old code: each crate declares its edition, and crates of different editions work together.
            - `rust-version` declares the oldest compiler your crate supports, so users with an older one get a clear message instead of a confusing error.
            - `[dev-dependencies]` are only used by tests, examples and benchmarks. They are never built into your program.

            ## Version requirements and SemVer
            Rust crates follow **Semantic Versioning** (SemVer): `MAJOR.MINOR.PATCH`. A patch release fixes bugs, a minor release adds features without breaking anything, and a major release may break existing users. Before 1.0, the *minor* number plays the "breaking" role: 0.4 to 0.5 may break.

            A version in `Cargo.toml` is a **requirement**, not an exact pin. `serde = "1.0.200"` means "1.0.200 or any later release compatible with it", that is, anything from 1.0.200 up to but not including 2.0.0. This is called a *caret* requirement and is Cargo's default. `"=1.0.200"` pins exactly, which is rarely right for libraries because it prevents your users from getting fixes.

            The promise behind this only works if crate authors respect SemVer, so when you publish a library, treat every `pub` item as part of a contract: removing or changing one is a major-version change.

            ## Cargo.lock: what was actually used
            When Cargo first resolves your dependencies it writes `Cargo.lock`, recording the exact version (and checksum) of every crate in the whole tree, including dependencies of dependencies. Later builds use exactly those versions, on any machine, until you choose to update.

            - **Commit `Cargo.lock`.** For programs this is essential: the build you tested is the build you ship. For libraries the Cargo team no longer advises leaving it out, and committing it keeps CI reproducible. (Users of your library resolve their own versions; your lock file doesn't affect them.)
            - `cargo update` moves to the newest versions your requirements allow and rewrites the lock file; `cargo update -p serde` updates one crate. Review the diff like any other change.
            - `cargo tree` prints the full dependency tree. `cargo tree -d` shows crates included in more than one version, a common source of slower builds and bigger binaries.

            ## Profiles: how the code is compiled
            The same source can be compiled very differently:

            - **`dev`**, used by `cargo build`, `cargo run` and `cargo test`: fast to compile, little optimization, full debug information, and **debug assertions on**. That includes `debug_assert!` checks and the integer-overflow panics from the number-types lesson.
            - **`release`**, used by `cargo build --release`: slow to compile, heavily optimized (computation-heavy code is often many times faster), and debug assertions and overflow checks **off**.

            Benchmark only release builds; measuring a dev build is measuring the wrong program. Profiles can be tuned in `Cargo.toml`:

            ```toml
            [profile.release]
            overflow-checks = true   # keep overflow panics in production
            debug = "line-tables-only" # readable backtraces from release binaries
            lto = "thin"             # optimize across crates
            ```

            Code can ask which kind of build it's in with `cfg!(debug_assertions)`, which is `true` in dev builds and `false` in release.

            ## Conditional compilation with cfg
            `cfg` is a compile-time condition. Code whose condition is false isn't compiled at all: it costs nothing and doesn't even need to type-check against the other configuration.

            - The attribute `#[cfg(...)]` includes or removes the item that follows: a function, a module, a `use` line or a statement. `#[cfg(test)]` from the testing lesson is one example.
            - The macro `cfg!(...)` evaluates to `true` or `false` in ordinary code, so both branches are compiled but only one runs. Use it when both branches are valid on every configuration.
            - Common conditions: `target_os = "linux"`, `target_os = "windows"`, `unix`, `windows`, `target_pointer_width = "64"`, `debug_assertions`, `test`, `feature = "name"`. Combine them with `all(...)`, `any(...)` and `not(...)`.

            ```rust
            #[cfg(unix)]
            fn config_dir() -> &'static str { "/etc/learner_tool" }

            #[cfg(windows)]
            fn config_dir() -> &'static str { "C:\\ProgramData\\learner_tool" }
            ```

            Systems code uses this constantly, because operating systems expose different interfaces.

            ## Features: optional parts of a crate
            A **feature** is a named switch defined in `Cargo.toml`. Users choose which features to enable, and your code checks them with `cfg(feature = "...")`.

            ```toml
            [features]
            default = []
            verbose = []
            metrics = ["dep:prometheus"]   # turning this on also pulls in an optional dependency

            [dependencies]
            prometheus = { version = "0.13", optional = true }
            ```

            Build with `cargo build --features verbose`, `--all-features`, or `--no-default-features`. Behind the scenes Cargo passes `--cfg feature="verbose"` to the compiler. That is exactly what this lesson's grader does, without Cargo.

            Two rules keep features sane. Features should be **additive**: turning one on adds capability and never breaks code that worked without it, because Cargo merges the features every crate in a build asks for. And remember each combination is a different program; CI should at least build with no features and with all of them.

            ## Quality tools
            - **`cargo fmt`** formats code in the standard style. `cargo fmt --check` fails if anything is unformatted, which is what CI should run. Formatting debates disappear because there's nothing to decide.
            - **`cargo clippy`** is the official linter, with hundreds of checks for likely bugs, slow patterns and non-idiomatic code, each linked to an explanation. Run `cargo clippy --all-targets -- -D warnings` in CI so warnings fail the build. Silence a specific lint only where you have a reason, with `#[allow(clippy::lint_name)]` and a comment saying why.
            - **`cargo doc --open`** builds HTML documentation from `///` comments for your crate and all its dependencies, which is often the fastest way to learn a dependency's API.
            - **`cargo audit`** (install with `cargo install cargo-audit`) checks your `Cargo.lock` against the RustSec database of known vulnerabilities. **`cargo deny`** goes further: it also checks licenses, banned crates and duplicate versions.

            Every dependency is code you're trusting to run inside your program. Before adding one, check that it's maintained, widely used, and that what it brings in (`cargo tree -p name`) is proportionate to what you need.

            ## A minimal CI script
            ```sh
            cargo fmt --check
            cargo clippy --all-targets --all-features -- -D warnings
            cargo test --all-features
            cargo test --no-default-features
            cargo audit
            ```

            ## Common mistakes
            - Not committing `Cargo.lock`, so CI and production quietly build different code.
            - Measuring performance with a debug build.
            - Features that remove behaviour (`no-logging`) instead of adding it, which break when another crate in the build enables something else.
            - Silencing Clippy across the whole crate instead of at the one place there's a reason.

            ## Try it locally
            1. Add `cargo add rand`, then open `Cargo.lock` and find every crate it brought in. Compare with `cargo tree`.
            2. Add the `verbose` feature to `Cargo.toml` and run the exercise code with and without `--features verbose`.
            3. Run `cargo clippy` on an earlier exercise and read at least one lint's linked explanation.
            4. Install and run `cargo audit` on a project.
            """,
        Example = """
            // Cargo.toml:  [features]  verbose = []
            fn log_level() -> &'static str {
                if cfg!(feature = "verbose") { "debug" } else { "info" }
            }

            #[cfg(debug_assertions)]
            fn build_profile() -> &'static str { "debug" }

            #[cfg(not(debug_assertions))]
            fn build_profile() -> &'static str { "release" }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Compile one source three ways",
            "Write three functions whose answers depend on how the code was *compiled*. The grader builds your file three times: a plain debug build, a debug build with the `metrics` feature, and an optimized release build with both `verbose` and `metrics`, exactly as Cargo would pass them. Don't write a `main` function.",
            [
                "`fn log_level() -> &'static str` returns `debug` when compiled with the `verbose` feature, otherwise `info`",
                "`fn build_profile() -> &'static str` returns `debug` for a build with debug assertions on and `release` for one with them off",
                "`fn enabled_features() -> Vec<&'static str>` lists the enabled features out of `verbose` and `metrics`, in that order (empty when neither is on)",
            ],
            """
            fn log_level() -> &'static str {
                todo!()
            }

            fn build_profile() -> &'static str {
                todo!()
            }

            fn enabled_features() -> Vec<&'static str> {
                todo!()
            }
            """,
            null,
            [],
            "Use compile-time checks, not environment variables: `cfg!(feature = \"verbose\")` and `cfg!(debug_assertions)` are `true` or `false` depending on the build. For the list, start with an empty `Vec` and `push` each feature whose `cfg!` is true.",
            ["Log level follows the verbose feature", "The profile reflects debug assertions", "The feature list matches the build"]),
    };
}
