namespace Pathway.Content;

static partial class Curriculum
{
    // Authored lessons for macros, structured concurrency, raw pointers, unsafe/FFI and atomics.
    // Fixtures live in sandbox-broker/src/evaluator.mjs. The raw-pointer and FFI graders also
    // require a `// SAFETY:` comment for every unsafe block in the submission.

    private static Lesson RustMacros(Lesson lesson) => lesson with
    {
        Subtitle = "Read and write `macro_rules!` macros: match syntax, repeat it, avoid evaluating arguments twice, and know when a function would be better.",
        Concept = "A declarative macro matches the code it's given against patterns and expands into new code before type checking. That lets it take a variable number of arguments, build syntax like map literals, and remove repetition functions can't. Macros are hygienic, so their internal names don't clash with yours, but an argument pasted twice is evaluated twice unless you bind it first.",
        Body = """
            ## Macros you already use
            `println!`, `format!`, `vec!`, `assert_eq!`, `write!`, `matches!`, `todo!`: every name ending in `!` is a **macro**. Each solves something a function can't:

            - `println!("{} and {}", a, b)` takes any number of arguments of any types and checks the format string against them **at compile time**.
            - `vec![1, 2, 3]` builds a `Vec` from a literal list of any length.
            - `assert_eq!(left, right)` includes the source text of the failing expressions in its message.

            A macro runs while the program is being compiled. It receives the code you wrote inside its brackets as **tokens**, and produces new code that replaces the macro call. The compiler then checks the result like any other code. Nothing is left of the macro at run time.

            Use `cargo expand` (install with `cargo install cargo-expand`) to see what a macro call expands into. It's the debugger for macros.

            ## macro_rules!
            **Declarative macros** are defined with `macro_rules!`. Each rule is a pattern and an expansion:

            ```rust
            macro_rules! square {
                ($value:expr) => {
                    $value * $value
                };
            }

            let nine = square!(3);
            ```

            `$value:expr` means "match one Rust expression and call it `$value`". The part after `:` is a **fragment specifier**:

            - `expr`: an expression, like `2 + 3` or `next()`.
            - `ident`: a name, like a variable or function name.
            - `ty`: a type, like `Vec<u8>`.
            - `tt`: a single token tree, the most flexible.
            - Also `pat`, `path`, `literal`, `block`, `stmt` and `item`.

            Expressions are kept as a unit, so `square!(2 + 3)` really is 25, not `2 + 3 * 2 + 3` = 11; that classic C preprocessor bug doesn't happen here.

            ## The double-evaluation trap
            But look again at that expansion: it pastes `$value` **twice**. `square!(next())` becomes `next() * next()`, calling the function two times. If it has side effects (reading input, incrementing a counter, popping from a queue), the result is wrong. The fix is to evaluate the argument once into a local variable:

            ```rust
            macro_rules! square {
                ($value:expr) => {{
                    let value = $value;
                    value * value
                }};
            }
            ```

            The doubled braces `{{ ... }}`: the outer pair delimits the expansion, and the inner pair is a **block expression** in the generated code, so the macro can declare a variable and still produce a value.

            Could the `value` inside the macro clash with a variable named `value` at the call site? No: `macro_rules!` is **hygienic**. Names the macro introduces live in their own context and can't capture or shadow the caller's names.

            ## Repetition
            Patterns can repeat with `$( ... ),*` (zero or more, separated by commas) or `$( ... ),+` (one or more), and the expansion repeats correspondingly:

            ```rust
            macro_rules! hashmap {
                () => {
                    ::std::collections::HashMap::new()
                };
                ($($key:expr => $value:expr),+ $(,)?) => {{
                    let mut map = ::std::collections::HashMap::new();
                    $( map.insert($key, $value); )+
                    map
                }};
            }

            let ages = hashmap! { "ada" => 36, "grace" => 85, };
            ```

            - The first rule handles an empty map.
            - `$($key:expr => $value:expr),+` matches one or more `key => value` pairs separated by commas. `=>` here is just a token the macro chose; it has no built-in meaning.
            - `$(,)?` matches an optional trailing comma, which people naturally write in long lists.
            - `$( map.insert($key, $value); )+` emits one `insert` per pair.
            - `::std::collections::HashMap` is a full path starting from the crate root, so the macro works even where the caller hasn't imported `HashMap`.

            Rules are tried top to bottom; the first that matches is used.

            ## Recursion
            A macro can call itself, peeling off one argument at a time:

            ```rust
            macro_rules! max_of {
                ($only:expr) => { $only };
                ($first:expr, $($rest:expr),+ $(,)?) => {{
                    let first = $first;
                    let rest = max_of!($($rest),+);
                    if first > rest { first } else { rest }
                }};
            }
            ```

            One argument is the base case. Otherwise, take the first, compute the maximum of the rest recursively, and compare. It works for any type that supports `>`, because the type check happens after expansion.

            ## Procedural macros
            The other kind of macro is a **procedural macro**: a Rust function, in its own crate, that receives tokens and returns tokens, with the full language available. They come in three forms:

            - **Derive** macros: `#[derive(Serialize)]`, `#[derive(Debug)]`. They generate trait implementations from a type's definition.
            - **Attribute** macros: `#[tokio::main]`, `#[test]`. They transform the item they're attached to.
            - **Function-like** macros: `sqlx::query!("SELECT ...")`, which can even check SQL against a database while compiling.

            Writing them uses the `syn` and `quote` crates, and is worth learning once you're comfortable with traits. Using them you already do.

            ## When not to write a macro
            Macros are harder to read, harder to debug, and give worse error messages and editor support than functions. Reach for a function, a generic or a trait first. Write a macro when you need something only a macro can do:

            - a variable number of arguments, or arguments of unrelated types;
            - new syntax, like a literal for a data structure;
            - generating many similar items (impls for a list of types, test cases for a list of inputs);
            - capturing source text or location (`stringify!`, `file!`, `line!`).

            ## Common mistakes
            - Pasting an `$expr` argument more than once.
            - Forgetting `$(,)?`, so a trailing comma or an empty call doesn't match.
            - Relative paths like `HashMap::new()` inside a macro, which break where the caller hasn't imported them.
            - Writing a macro where a generic function would do.

            ## Try it locally
            1. Write the first `square!`, call it with a closure that prints, and count the prints. Fix it.
            2. Run `cargo expand` on a file using `hashmap!` and read the generated code.
            3. Run `cargo expand` on a `#[derive(Debug)]` struct to see what a derive generates.
            """,
        Example = """
            macro_rules! hashmap {
                () => { ::std::collections::HashMap::new() };
                ($($key:expr => $value:expr),+ $(,)?) => {{
                    let mut map = ::std::collections::HashMap::new();
                    $( map.insert($key, $value); )+
                    map
                }};
            }

            let ages = hashmap! { "ada" => 36, "grace" => 85 };
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Write three declarative macros",
            "Define three `macro_rules!` macros. The grader calls them with literals, expressions with side effects, trailing commas and different types. Don't write a `main` function.",
            [
                "`square!(expr)` returns the expression multiplied by itself, evaluating the expression exactly once",
                "`hashmap! { key => value, ... }` builds a `std::collections::HashMap`, accepting a trailing comma, and `hashmap! {}` builds an empty one",
                "`max_of!(a, b, ...)` returns the largest of one or more values of any comparable type, accepting a trailing comma",
            ],
            """
            macro_rules! square {
                ($value:expr) => {
                    todo!()
                };
            }

            macro_rules! hashmap {
                // An empty rule, and a rule for one or more `key => value` pairs.
            }

            macro_rules! max_of {
                // A rule for one value, and a recursive rule for more.
            }
            """,
            null,
            [],
            "For `square!`, expand into a block `{{ let value = $value; value * value }}`. For `hashmap!`, use `$($key:expr => $value:expr),+ $(,)?` and repeat `map.insert($key, $value);` with `$( ... )+`, plus a separate `() => {...}` rule. For `max_of!`, one rule for `($only:expr)` and one that compares `$first` with `max_of!($($rest),+)`.",
            ["Arguments are evaluated once", "Map literals accept any number of pairs", "Recursion handles any number of values"]),
    };

    private static Lesson RustStructuredConcurrency(Lesson lesson) => lesson with
    {
        Subtitle = "Keep every spawned task owned: wait for groups of tasks, cancel the rest when one fails, and shut workers down on a signal.",
        Concept = "Structured concurrency means no task outlives the code that started it unless that is a deliberate decision. A `JoinSet` owns a group of tasks: it collects their results as they finish and aborts whatever is still running when it's dropped. Long-running workers listen for a shutdown signal with `tokio::select!`, so they stop promptly instead of waiting for work that will never come.",
        Body = """
            ## The problem with detached tasks
            The async lesson used `tokio::spawn` to run work concurrently. `spawn` returns a `JoinHandle`, but dropping that handle does **not** stop the task; it keeps running in the background, detached. That's convenient and dangerous:

            - When an operation fails partway, its sibling tasks carry on, using CPU, holding database connections and possibly writing results nobody wants.
            - A request handler can return while tasks it started are still running, so work outlives the request it belonged to.
            - Errors and panics in detached tasks go unnoticed unless something awaits their handle.

            **Structured concurrency** is the discipline that fixes this: every task has an owner, the owner waits for it or cancels it, and when a piece of code returns, all the concurrent work it started is finished. It's the concurrent version of the ownership rules: no orphans.

            ## Cancellation in async Rust
            A future does nothing unless it's polled. So **dropping a future cancels it**: it simply stops at whichever `.await` it was paused on, and its local values are dropped normally, running their destructors. For a spawned task, `handle.abort()` asks the runtime to drop it at its next `.await`.

            That makes cancellation cheap, but it means every `.await` is a point where your function might stop and never resume. Code that does "step 1, await, step 2" must be correct even if it stops after step 1. Design for that: write to a temporary location and rename (the files lesson), use transactions, or make operations idempotent so repeating them is harmless.

            ## JoinSet: an owned group of tasks
            `tokio::task::JoinSet` holds a set of spawned tasks:

            ```rust
            use tokio::task::JoinSet;

            let mut set = JoinSet::new();
            for id in ids {
                set.spawn(fetch(id));
            }
            while let Some(joined) = set.join_next().await {
                let value = joined?;   // a JoinError if the task panicked or was aborted
                // ...
            }
            ```

            - `spawn` adds a task. The tasks run concurrently, as with `tokio::spawn`.
            - `join_next().await` returns the next task to **finish**, whichever it is, or `None` when the set is empty. Results arrive in completion order, so to restore input order, have each task return its index alongside its result.
            - **Dropping the `JoinSet` aborts every task still in it.** That's the key property.

            Together they give **fail-fast** behaviour for free. If you return early with `?` when one task's result is an error, the `JoinSet` goes out of scope, and every other task is cancelled:

            ```rust
            while let Some(joined) = set.join_next().await {
                let (index, outcome) = joined.map_err(|e| e.to_string())?;
                results[index] = outcome?;   // on Err: return now, and the set aborts the rest
            }
            ```

            Compare a `Vec` of `tokio::spawn` handles with the same early return: the function returns the error, but the remaining tasks keep running, detached.

            ## Workers and shutdown signals
            Many services run long-lived **workers**: loops that receive jobs from a channel and process them. They need two ways to stop:

            1. The job channel closes (every sender was dropped): there's no more work.
            2. The application is shutting down: stop *now*, even though the channel is still open.

            The second needs a signal the worker can wait on at the same time as it waits for jobs. `tokio::select!` waits on several futures at once and runs the branch of whichever finishes first, cancelling the others:

            ```rust
            use tokio::sync::{mpsc, watch};

            async fn worker(mut jobs: mpsc::Receiver<u32>, mut shutdown: watch::Receiver<bool>) -> u32 {
                let mut total = 0;
                loop {
                    tokio::select! {
                        changed = shutdown.changed() => {
                            if changed.is_err() || *shutdown.borrow() {
                                return total;   // shutdown requested, or its sender is gone
                            }
                        }
                        job = jobs.recv() => match job {
                            Some(value) => total += value,
                            None => return total,   // channel closed: no more work
                        },
                    }
                }
            }
            ```

            - A **`watch` channel** holds a single value that many receivers can observe; `changed().await` waits until it's updated. It's a natural fit for a shutdown flag broadcast to many workers. (The `tokio-util` crate's `CancellationToken` is a purpose-built alternative.)
            - Because `select!` cancels the losing branches, each branch should be **cancel-safe**: dropping it mid-wait must not lose data. `mpsc::Receiver::recv` and `watch::Receiver::changed` are both documented as cancel-safe. Check the documentation of anything you put in a `select!`.
            - The job currently being processed (the `total += value` line here) completes before the loop checks for shutdown again. If processing a job involves long `.await`s, decide explicitly whether shutdown should interrupt it.

            A whole service's graceful shutdown is these pieces composed: on `SIGTERM` (or `tokio::signal::ctrl_c()`), stop accepting new work, send the shutdown signal, wait for workers with a deadline (`tokio::time::timeout` around joining them), then exit.

            ## Limits and backpressure
            The async lesson capped concurrency with a semaphore. The same thinking applies to queues: an unbounded channel lets producers outrun consumers until memory runs out. A **bounded** channel, `mpsc::channel(capacity)`, makes `send().await` wait when the channel is full. That waiting is **backpressure**: the slowdown travels back to the producer instead of piling up in memory. Every queue in a system should have a bound and a decision about what happens when it's full: wait, drop, or reject with an error.

            ## Common mistakes
            - Fire-and-forget `tokio::spawn` for work that belongs to a request or operation.
            - Returning an error while sibling tasks keep running.
            - Workers that only stop when their channel closes, so shutdown hangs.
            - Putting non-cancel-safe operations in `select!`.
            - Unbounded channels between producers and consumers.

            ## Try it locally
            1. Spawn five tasks with `tokio::spawn` that print after different delays, return from `main` early, and see which print. Repeat with a `JoinSet`.
            2. Build the worker, send jobs, then trigger shutdown with `tokio::signal::ctrl_c()` instead of a test.
            3. Replace a bounded channel with an unbounded one, make the consumer slow, and watch memory grow.
            """,
        Example = """
            use tokio::task::JoinSet;

            let mut set = JoinSet::new();
            for (index, id) in ids.iter().copied().enumerate() {
                set.spawn(async move { (index, fetch(id).await) });
            }
            let mut results = vec![0; ids.len()];
            while let Some(joined) = set.join_next().await {
                let (index, outcome) = joined.map_err(|e| e.to_string())?;
                results[index] = outcome?; // an early return drops the set and aborts the rest
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Fail fast and shut down cleanly",
            "Write a fail-fast runner that owns its tasks, and a worker that stops on a shutdown signal. The grader provides `async fn job(id: u32, done: Arc<AtomicUsize>) -> Result<u32, String>`, which sleeps `id * 10` ms, fails for id 3, and counts successful jobs. Tokio is available. Don't write a `main` function.",
            [
                "`async fn run_all(ids: Vec<u32>, done: Arc<AtomicUsize>) -> Result<Vec<u32>, String>` runs `job` for every id concurrently and returns the results in input order, or the first error",
                "When a job fails, `run_all` returns immediately and every job still running is cancelled (slow jobs must never finish in the background)",
                "`async fn worker(jobs: mpsc::Receiver<u32>, shutdown: watch::Receiver<bool>) -> u32` adds up the jobs it receives and returns the total when the channel closes, or promptly when `true` is sent on `shutdown`, even if the channel is still open",
            ],
            """
            use std::sync::Arc;
            use std::sync::atomic::AtomicUsize;
            use tokio::sync::{mpsc, watch};
            use tokio::task::JoinSet;

            // The grader provides: async fn job(id: u32, done: Arc<AtomicUsize>) -> Result<u32, String>

            async fn run_all(ids: Vec<u32>, done: Arc<AtomicUsize>) -> Result<Vec<u32>, String> {
                todo!()
            }

            async fn worker(mut jobs: mpsc::Receiver<u32>, mut shutdown: watch::Receiver<bool>) -> u32 {
                todo!()
            }
            """,
            null,
            [],
            "Spawn each job into a `JoinSet` returning `(index, result)`, then loop on `join_next().await`, using `?` on the result: the early return drops the set and aborts the rest. For the worker, loop over `tokio::select!` with one branch on `shutdown.changed()` and one on `jobs.recv()`.",
            ["Results keep input order", "A failure cancels the remaining jobs", "Workers stop on shutdown or when work ends"]),
    };

    private static Lesson RustMemoryAddressesRaw(Lesson lesson) => lesson with
    {
        Title = "Raw pointers and your first unsafe code",
        Subtitle = "Learn what a raw pointer is, what `unsafe` actually permits, and how to write an unsafe operation behind a safe function with a written proof that it's sound.",
        Concept = "A raw pointer (`*const T`, `*mut T`) is an address with none of a reference's guarantees: it may be null, dangling, misaligned or aliased. Creating one is safe; dereferencing one requires `unsafe`, which means \"I have checked the rules the compiler can't\". The standard practice is to keep unsafe operations tiny, wrap them in safe functions that check every precondition, and document each with a `// SAFETY:` comment.",
        Body = """
            ## References are pointers with promises
            A `&T` is an address, but it comes with guarantees the compiler enforces: it's never null, it points at a valid, initialized `T`, it's properly aligned, the value lives at least as long as the reference, and the borrowing rules hold (shared or exclusive, never both). Everything in the memory-safety module rests on those promises.

            A **raw pointer** is the same address without the promises:

            - `*const T`: a pointer you may read through.
            - `*mut T`: a pointer you may write through.

            Raw pointers can be null, point at freed memory, point past the end of an array, be misaligned, or alias a `&mut`. They have no lifetime, so the borrow checker doesn't track them. They exist for the places where those guarantees can't be expressed to the compiler: talking to C, writing allocators and data structures like `Vec` itself, memory-mapped hardware registers.

            ## Creating pointers is safe
            ```rust
            let values = [10u32, 20, 30];
            let first: *const u32 = values.as_ptr();
            let also_first = &values[0] as *const u32;
            let address = first as usize;     // the address as a number
            println!("{first:p}");           // print a pointer
            ```

            None of this needs `unsafe`. A pointer is just a number until you use it to touch memory. Comparing pointers, printing them and doing arithmetic on them is safe.

            Pointer arithmetic counts in **elements, not bytes**. `first.add(1)` points at the next `u32`, 4 bytes further on. Converting both to `usize` and subtracting gives the distance in bytes, which for neighbouring elements is `size_of::<T>()`. `offset_from` gives the distance in elements.

            ## Dereferencing requires unsafe
            ```rust
            let second = unsafe { *first.add(1) };
            ```

            An `unsafe` block does **not** turn off the borrow checker or any other check. It unlocks exactly five extra abilities:

            1. Dereference a raw pointer.
            2. Call an `unsafe fn` (including foreign C functions).
            3. Access or modify a mutable `static`.
            4. Implement an `unsafe trait` (such as `Send`, `Sync` or `GlobalAlloc`).
            5. Access fields of a `union`.

            By writing `unsafe`, you take over responsibility for rules the compiler can't verify. If you get them wrong, the result is **undefined behavior** (UB): the compiler assumed those rules hold while optimizing, so anything can happen. Not "it crashes": it may appear to work, produce wrong results later, behave differently in release builds, or become exploitable. UB is the source of the memory-safety vulnerabilities Rust exists to prevent.

            ## The rules for dereferencing a pointer
            Before `*ptr` or `*ptr.add(i)`, all of these must hold:

            - The pointer is **non-null** and **aligned** for `T`.
            - It points **inside a live allocation**, with room for a whole `T`; `add(i)` must stay within the same allocation (or exactly one past its end, without being dereferenced there).
            - The memory holds a **valid, initialized** `T`.
            - The **aliasing rules** hold: you don't read through a pointer while something else has a `&mut` to the same memory, and you don't write while any reference to it exists.

            ## unsafe fn and the safe wrapper
            When a function's correctness depends on its caller upholding such rules, mark it `unsafe fn` and document them under a `# Safety` heading:

            ```rust
            /// Reads the `index`th `u32` after `ptr`.
            ///
            /// # Safety
            /// `ptr` must point to at least `index + 1` initialized, aligned `u32`s
            /// that stay valid for the duration of the call.
            unsafe fn read_nth(ptr: *const u32, index: usize) -> u32 {
                // SAFETY: the caller guarantees `ptr.add(index)` is in bounds.
                unsafe { *ptr.add(index) }
            }
            ```

            Then offer a **safe** function that checks the preconditions itself, so callers can't get it wrong:

            ```rust
            fn checked_read(values: &[u32], index: usize) -> Option<u32> {
                if index >= values.len() {
                    return None;
                }
                // SAFETY: index < values.len(), and a slice's elements are initialized,
                // aligned, and borrowed for the whole call.
                Some(unsafe { read_nth(values.as_ptr(), index) })
            }
            ```

            This is how the whole standard library works: `Vec`, `String` and `HashMap` are built on unsafe code, behind safe APIs that are **sound**, meaning no safe caller can trigger UB through them, whatever arguments they pass. That's the bar for any safe function containing `unsafe`.

            ## SAFETY comments
            The universal Rust convention: every `unsafe` block gets a `// SAFETY:` comment saying *why* each rule holds at that point, and every `unsafe fn` gets a `# Safety` section saying what its callers must guarantee. Reviewers check the reasoning, not just the code. Clippy can enforce it with the `undocumented_unsafe_blocks` lint. Since Rust 2024, the body of an `unsafe fn` is no longer implicitly unsafe; each unsafe operation inside needs its own `unsafe { }` block and justification. This lesson's grader checks for those comments.

            ## Common mistakes
            - Treating `unsafe` as "turn off the checks" rather than "I've proved the checks I'm responsible for".
            - A safe function that passes unchecked caller input into an unsafe operation, which makes it unsound.
            - Assuming pointer arithmetic is in bytes.
            - Writing unsafe code to win a performance argument without measuring; safe code with bounds checks is usually within noise, and the optimizer removes many checks.

            ## Try it locally
            1. Print the addresses of the elements of a `[u64; 3]` and a `[u8; 3]` with `{:p}` and explain the spacing.
            2. Write `checked_read` without the bounds check, call it with an out-of-range index, and run it under **Miri** (`rustup +nightly component add miri`, then `cargo +nightly miri run`). Read the error it reports. The next lesson covers Miri properly.
            3. Find an `unsafe` block in the standard library source (`Vec::push` is a good one, via your editor's go-to-definition) and read its SAFETY comment.
            """,
        Example = """
            fn checked_read(values: &[u32], index: usize) -> Option<u32> {
                if index >= values.len() {
                    return None;
                }
                // SAFETY: index < values.len(), so the pointer stays inside the slice,
                // whose elements are initialized and aligned.
                Some(unsafe { *values.as_ptr().add(index) })
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Wrap an unsafe read in a safe function",
            "Write an `unsafe fn` that reads through a raw pointer, a safe wrapper that makes it impossible to misuse, and a function that measures pointer distance. Every `unsafe { }` block you write must have a `// SAFETY:` comment. Don't write a `main` function.",
            [
                "`unsafe fn read_nth(ptr: *const u32, index: usize) -> u32` returns the `index`th `u32` after `ptr`, with a `# Safety` doc section",
                "`fn checked_read(values: &[u32], index: usize) -> Option<u32>` returns `None` for an index outside the slice, and otherwise calls `read_nth`",
                "`fn element_stride(values: &[u64]) -> usize` returns the distance **in bytes** between the addresses of the first two elements, or 0 if there are fewer than two",
                "Every `unsafe { }` block is preceded by a `// SAFETY:` comment",
            ],
            """
            /// # Safety
            /// Describe what callers must guarantee.
            unsafe fn read_nth(ptr: *const u32, index: usize) -> u32 {
                todo!()
            }

            fn checked_read(values: &[u32], index: usize) -> Option<u32> {
                todo!()
            }

            fn element_stride(values: &[u64]) -> usize {
                todo!()
            }
            """,
            null,
            [],
            "In `read_nth`, `unsafe { *ptr.add(index) }` with a SAFETY comment. In `checked_read`, return `None` when `index >= values.len()` before calling it. For the stride, convert `&values[0] as *const u64` and `&values[1] as *const u64` to `usize` and subtract; no unsafe is needed.",
            ["Reads through a raw pointer in elements", "Out-of-range indexes never reach the unsafe read", "Pointer distance is measured in bytes"]),
    };

    private static Lesson RustUnsafeFfi(Lesson lesson) => lesson with
    {
        Title = "Unsafe, FFI, and checking soundness with Miri",
        Subtitle = "Call C from Rust and expose Rust to C, translate between their types and rules at the boundary, and use Miri to catch undefined behavior that tests can't see.",
        Concept = "A foreign function interface (FFI) lets Rust call code compiled from C (and vice versa) using the C calling convention. The compiler can't check anything on the other side, so every foreign call is unsafe, and the Rust wrapper must enforce C's rules: NUL-terminated strings, valid pointers, and no inputs that are undefined behavior in C. Miri, an interpreter for Rust's intermediate representation, detects many kinds of undefined behavior when you run your tests under it.",
        Body = """
            ## Why FFI matters
            Huge amounts of essential software are written in C: operating system interfaces, compression and crypto libraries, databases like SQLite, device drivers. Systems programmers routinely need to call that code from Rust, or make Rust callable from C, Python, or any language that speaks C's **ABI** (application binary interface: how arguments are passed in registers and on the stack, how structs are laid out, who cleans up). Rust's standard library itself calls the C library for most system calls.

            ## Calling C
            Declare the foreign functions in an `extern "C"` block. Since the 2024 edition the block itself is written `unsafe extern`, because the declaration is a promise the compiler can't check: that these signatures match the real functions.

            ```rust
            use std::ffi::{c_char, c_int};

            unsafe extern "C" {
                fn strlen(text: *const c_char) -> usize;
                fn abs(value: c_int) -> c_int;
            }
            ```

            Calling them requires `unsafe`, since Rust can't see what they do with their arguments. Functions from the C standard library link automatically on most platforms; for other libraries, Cargo's build script (`build.rs`) tells the linker what to link, and the `cc` crate can compile C sources as part of your build.

            For real libraries you rarely write declarations by hand. **bindgen** reads a C header file and generates the Rust declarations, and **cbindgen** goes the other way, generating a C header from your Rust code. The convention is a `-sys` crate (like `libsqlite3-sys`) holding the raw bindings, and a separate crate with a safe, idiomatic wrapper on top.

            ## Translating types
            C and Rust disagree about basic things, so the boundary is where translation happens:

            - **Integers**: use `std::ffi::c_int`, `c_long`, `c_char` and friends, because C's `int` and `long` sizes vary by platform. `usize` matches C's `size_t`.
            - **Strings**: a Rust `&str` is UTF-8 with a length and no terminator. A C string is a pointer to bytes ending at the first **NUL** (zero) byte, with no length and no promise about encoding. `CString::new(text)` makes an owned, NUL-terminated copy to pass to C, and **fails if the text contains a NUL byte**, since C would see the string end early. `CStr::from_ptr(ptr)` views a C string from Rust (unsafe: you promise the pointer is valid and terminated), and `.to_str()` checks it's valid UTF-8.
            - **Structs**: mark them `#[repr(C)]` so their layout matches C's (the stack-and-heap lesson).
            - **Ownership**: C has no ownership rules, so documentation decides who frees what. Memory allocated by C must be freed by the matching C function, and memory Rust allocated must be returned to Rust to free. Mixing allocators is UB.
            - **Lifetimes**: `CString::new(text)?.as_ptr()` in one expression is a classic bug: the `CString` is a temporary dropped at the end of the statement, leaving the pointer dangling. Bind the `CString` to a variable that lives as long as C uses the pointer.

            ## C's undefined behavior is your problem now
            A safe Rust wrapper must stop *every* input that would be UB on the C side, not just the Rust-side ones. A small but instructive example: in C, `abs(INT_MIN)` is undefined behavior, because the positive value doesn't fit in an `int`. A wrapper that passes any `i32` straight through is **unsound**: a safe caller can trigger UB. The sound wrapper refuses that input, returning `None` (or uses `i32::checked_abs`, the pure-Rust equivalent). Reading the C documentation's preconditions and enforcing each one in the wrapper is most of the work of writing FFI bindings.

            Also never let a Rust panic unwind across an `extern "C"` boundary into C code; it is not designed for that. Since Rust 1.81 an unwind reaching an `extern "C"` function aborts the process. If a callback might panic, catch it with `std::panic::catch_unwind` and return an error code.

            ## Exposing Rust to C
            The other direction uses an attribute and a signature C can call:

            ```rust
            #[unsafe(no_mangle)]
            pub extern "C" fn pathway_checksum(data: *const u8, len: usize) -> u32 {
                if data.is_null() {
                    return 0;
                }
                // SAFETY: the caller guarantees `data` points to `len` readable bytes.
                let bytes = unsafe { std::slice::from_raw_parts(data, len) };
                bytes.iter().map(|&b| u32::from(b)).sum()
            }
            ```

            `#[unsafe(no_mangle)]` keeps the symbol name exactly `pathway_checksum` so C can find it (the `unsafe(...)` form, required in the 2024 edition, marks that a name collision could break linking). Set `crate-type = ["cdylib"]` or `["staticlib"]` in `Cargo.toml` to build a library C programs can link.

            ## Miri: finding UB that tests miss
            UB often doesn't crash. A test that reads one element past a slice usually gets *some* number and passes. **Miri** is an interpreter that runs your program (normally your test suite) step by step, tracking every allocation, every pointer's permissions and every byte's initialization, and stops with a precise report the moment a rule is broken: out-of-bounds access, use after free, reading uninitialized memory, misaligned access, data races, and aliasing violations under Rust's Stacked/Tree Borrows models.

            ```sh
            rustup +nightly component add miri
            cargo +nightly miri test
            ```

            It's slow (often 100 times slower or more) and it can't run foreign C code, so test the Rust side of your unsafe abstractions with it, keeping test inputs small. Every crate with `unsafe` code should run Miri in CI. Complementary tools: the sanitizers (`RUSTFLAGS=-Zsanitizer=address cargo +nightly test`) catch memory errors in real machine code, including across FFI, and fuzzing (the binary-parser lesson) finds the inputs that trigger them.

            ## Common mistakes
            - A pointer from a temporary `CString` that's already been dropped.
            - Passing Rust text with an interior NUL to C, or `unwrap`ping `CString::new` on untrusted text.
            - Forwarding inputs that are UB in C because they're fine in Rust.
            - Freeing memory with the wrong allocator.
            - Shipping unsafe code that has never run under Miri.

            ## Try it locally
            1. Call `strlen` on a `CString` and on a temporary `CString::new("x").unwrap().as_ptr()` stored in a variable. Run both under Miri and read its dangling-pointer report.
            2. Generate bindings for a small C header with `bindgen` and read the output.
            3. Build a `cdylib` exposing `pathway_checksum`, and call it from a 10-line C program or from Python's `ctypes`.
            """,
        Example = """
            use std::ffi::{CString, c_char};

            unsafe extern "C" {
                fn strlen(text: *const c_char) -> usize;
            }

            fn c_strlen(text: &str) -> Option<usize> {
                let owned = CString::new(text).ok()?; // None if text contains a NUL byte
                // SAFETY: `owned` is NUL-terminated and lives until the end of this function.
                Some(unsafe { strlen(owned.as_ptr()) })
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Write sound wrappers around C functions",
            "Declare two functions from the C standard library and wrap them so that no safe caller can cause undefined behavior, then read a C string back into Rust. Every `unsafe { }` block you write needs a `// SAFETY:` comment. Don't write a `main` function.",
            [
                "Declare `strlen` and `abs` in an `unsafe extern \"C\"` block. `fn c_strlen(text: &str) -> Option<usize>` returns C's length of the text, or `None` if the text contains a NUL byte",
                "`fn c_abs(value: i32) -> Option<i32>` calls C's `abs`, returning `None` for `i32::MIN`, the one input that is undefined behavior in C",
                "`unsafe fn from_c_string(ptr: *const c_char) -> Option<String>` returns `None` for a null pointer or bytes that aren't valid UTF-8; document its `# Safety` contract",
                "Every `unsafe { }` block is preceded by a `// SAFETY:` comment",
            ],
            """
            use std::ffi::{CStr, CString, c_char, c_int};

            unsafe extern "C" {
                // Declare strlen and abs here.
            }

            fn c_strlen(text: &str) -> Option<usize> {
                todo!()
            }

            fn c_abs(value: i32) -> Option<i32> {
                todo!()
            }

            /// # Safety
            /// Describe what callers must guarantee about `ptr`.
            unsafe fn from_c_string(ptr: *const c_char) -> Option<String> {
                todo!()
            }
            """,
            null,
            [],
            "`fn strlen(text: *const c_char) -> usize;` and `fn abs(value: c_int) -> c_int;`. Keep the `CString` in a variable while C uses its pointer, and use `.ok()?` instead of `unwrap`. Check `value == i32::MIN` before calling `abs`. In `from_c_string`, check `ptr.is_null()` first, then `CStr::from_ptr(ptr).to_str().ok()`.",
            ["C strings are built safely", "Inputs that are UB in C are refused", "C strings are read back with checks"]),
    };

    private static Lesson RustAtomicsLocks(Lesson lesson) => lesson with
    {
        Subtitle = "Share state between threads without losing updates: atomic read-modify-write operations, compare-and-swap, memory ordering, and locking in a consistent order to avoid deadlock.",
        Concept = "An atomic operation happens as one indivisible step, so concurrent increments or claims can't interleave and lose data. Memory ordering decides what other memory writes become visible along with an atomic one. Mutexes protect larger state, and deadlocks are prevented by always acquiring multiple locks in one global order.",
        Body = """
            ## The lost update
            The concurrency lesson showed that Rust won't let two threads mutate shared data without synchronization. It's worth seeing exactly what goes wrong without it. Incrementing a counter is really three steps: **read** the value, **add** one, **write** it back. If two threads both read 5, both compute 6 and both write 6, one increment has vanished. Run that a million times across eight threads and the total comes out wildly short. This **race condition** is the same bug as two people editing one document from stale copies.

            An `AtomicU64::load` followed by a separate `store` has exactly this bug, even though each step is atomic on its own. Atomicity has to cover the whole read-modify-write.

            ## Atomic read-modify-write
            `std::sync::atomic` provides types like `AtomicBool`, `AtomicUsize` and `AtomicU64` whose operations the processor performs as **one indivisible step** using special instructions:

            - `fetch_add(1, ordering)`: add and return the previous value, atomically. No increment is ever lost.
            - `fetch_sub`, `fetch_or`, `fetch_and`, `fetch_max`, ...
            - `swap(new, ordering)`: store a new value and return the old one.
            - `compare_exchange(expected, new, success_order, failure_order)`: **compare-and-swap** (CAS). "If the value is still `expected`, replace it with `new`." It returns `Ok(previous)` if it succeeded, or `Err(actual)` if another thread changed it first.

            Atomics only need `&self`, not `&mut self`, so they can be shared freely between threads, for example in an `Arc` or a `static`.

            CAS is the building block for lock-free code. Claiming something exactly once is a single CAS from `false` to `true`: of any number of threads that try at the same moment, exactly one sees `Ok`. A "check, then set" version (`if !flag.load() { flag.store(true) }`) has a gap between the check and the set, and two threads can both pass the check. (Here `swap(true)` returning the old value works too.) For "initialize once", the standard library already provides `std::sync::OnceLock`; build your own only to learn the mechanism.

            ## Memory ordering
            Processors and compilers reorder memory operations for speed, as long as a *single* thread can't tell. Another thread can tell. The `Ordering` argument says how much an atomic operation constrains that reordering:

            - **`Relaxed`**: the operation itself is atomic, but it says nothing about other memory. Correct for counters and statistics, where only the number matters.
            - **`Release`** (on a store) and **`Acquire`** (on a load) work as a pair: everything a thread wrote before a `Release` store is visible to a thread that `Acquire`-loads that value. This is how one thread **publishes** data to another: write the data, then set a "ready" flag with `Release`; the reader checks the flag with `Acquire` and can then safely read the data.
            - **`AcqRel`**: both, for read-modify-write operations like a CAS that both observes and publishes.
            - **`SeqCst`**: the strongest; all `SeqCst` operations appear in one global order every thread agrees on. A safe default when unsure, and slightly slower.

            Getting ordering wrong produces bugs that appear only under load, on some processors (ARM reorders more aggressively than x86), once in a million runs. The practical rule: use a `Mutex` or a channel unless profiling shows a real need for atomics, and when you do use them, write down why the chosen ordering is enough. Mara Bos's free book *Rust Atomics and Locks* is the standard deep reference.

            ## Mutexes and lock ordering
            For state larger than one number, a `Mutex` is simpler and safer: the guard from `lock()` gives exclusive access and unlocks when dropped. A `lock()` returns `Err` only if another thread **panicked** while holding the lock (the mutex is "poisoned"); `unwrap` is a reasonable response in most programs, since the data may be inconsistent.

            The classic danger with several locks is **deadlock**. Thread A locks account 1 and waits for account 2; thread B locks account 2 and waits for account 1. Neither can proceed, forever. Rust prevents data races at compile time but **not deadlocks**; those are logic errors.

            The standard prevention is a **global lock order**: whenever code needs more than one lock, it acquires them in the same agreed order, so a cycle of waiting can't form. When the locks have no natural order (two accounts passed as arguments in either order), order by something both threads agree on, such as an account ID, or the locks' memory addresses:

            ```rust
            fn transfer(from: &Mutex<i64>, to: &Mutex<i64>, amount: i64) {
                if std::ptr::eq(from, to) {
                    return;   // locking the same mutex twice would deadlock immediately
                }
                let from_first = (from as *const Mutex<i64>) < (to as *const Mutex<i64>);
                let (mut source, mut target) = if from_first {
                    let source = from.lock().unwrap();
                    let target = to.lock().unwrap();
                    (source, target)
                } else {
                    let target = to.lock().unwrap();
                    let source = from.lock().unwrap();
                    (source, target)
                };
                *source -= amount;
                *target += amount;
            }
            ```

            Note the special case: `std::sync::Mutex` is not reentrant, so a thread locking a mutex it already holds deadlocks itself on the spot.

            Other ways to reduce lock problems: hold locks for as short a time as possible, never call unknown code (callbacks, I/O) while holding one, and prefer one lock over several when the extra concurrency isn't measured to matter. `RwLock` allows many readers or one writer, which helps read-heavy data.

            ## Common mistakes
            - `load` then `store` instead of `fetch_add` or `compare_exchange`.
            - Check-then-set for "only once" logic.
            - `Relaxed` used to publish data to another thread.
            - Acquiring several locks in an order that depends on the arguments.
            - Holding a lock across slow I/O.

            ## Try it locally
            1. Count to a million across eight threads with `load` + `store`, then with `fetch_add`, in a release build.
            2. Write the naive two-lock `transfer`, run opposite transfers in two threads, and watch it hang. Then apply the address ordering.
            3. Run a small atomics example under Miri, which explores some thread interleavings and reports data races.
            """,
        Example = """
            use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};

            static REQUESTS: AtomicU64 = AtomicU64::new(0);
            static STARTED: AtomicBool = AtomicBool::new(false);

            REQUESTS.fetch_add(1, Ordering::Relaxed); // never loses an increment
            let we_won = STARTED
                .compare_exchange(false, true, Ordering::AcqRel, Ordering::Acquire)
                .is_ok();                            // exactly one caller sees true
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Count, claim, and transfer without races or deadlocks",
            "Build a lock-free counter, a claim-once flag, and a deadlock-free transfer between two mutex-protected balances. The grader hammers each from many threads and stops a run that hangs. Don't write a `main` function.",
            [
                "`struct Counter` with `fn new() -> Counter`, `fn increment(&self)` and `fn get(&self) -> u64`, losing no increments when 8 threads increment at once",
                "`struct OnceFlag` with `fn new() -> OnceFlag` and `fn try_claim(&self) -> bool`, where exactly one of any number of simultaneous callers gets `true`",
                "`fn transfer(from: &Mutex<i64>, to: &Mutex<i64>, amount: i64)` moves the amount between balances, never deadlocks when two threads transfer in opposite directions, and does nothing when `from` and `to` are the same mutex",
            ],
            """
            use std::sync::Mutex;
            use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};

            struct Counter {
                value: AtomicU64,
            }

            struct OnceFlag {
                claimed: AtomicBool,
            }

            // impl Counter { ... }  impl OnceFlag { ... }

            fn transfer(from: &Mutex<i64>, to: &Mutex<i64>, amount: i64) {
                todo!()
            }
            """,
            null,
            [],
            "`fetch_add(1, Ordering::Relaxed)` for the counter; `compare_exchange(false, true, Ordering::AcqRel, Ordering::Acquire).is_ok()` for the flag. In `transfer`, return early when `std::ptr::eq(from, to)`, then lock the mutex with the lower address first.",
            ["No increment is lost", "Exactly one claim succeeds", "Opposite transfers never deadlock"]),
    };
}
