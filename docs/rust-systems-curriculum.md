# Rust Systems: practical to low-level expertise

**Audience:** developers coming from C#, Java, JavaScript, or Python who want independent, technically defensible systems-programming competence. Rust is not a shortcut to mastery of hardware or OS internals; those concepts need practice and experiments.

## Learning contract

Each module should define vocabulary *before* using it. Teach mechanisms, work through a small runnable example, then assign an independent exercise. Prefer realistic errors and tests over immediate recall quizzes. Reserve graded assessments for the **end of a module**, after meaningful instructional content and practice. Treat early exercises in the existing sequence as formative checkpoints, not proof of mastery. The present application still models many lessons with their own exercise; a separate module-assessment UI/content refactor remains necessary.

- **Stage 1 — First useful program.** Rustup, rustc, Cargo, OS-specific installation and native build prerequisites, editor setup, first CLI, functions, types, and Result. Learner can independently create, run, debug, and test a binary crate.
- **Stage 2 — Rust's mental model.** Ownership, moves, borrowing, drop, lifetimes, enums, traits, iterators, smart pointers. Compare each concept with managed-language behavior; use compiler errors as teaching evidence.
- **Stage 3 — Low-level foundations.** Stack versus heap; alignment and padding; bytes/bit operations, overflow and endian formats; arrays, Vec, slices, allocation; files and system calls; processes and pipes; pointers and aliasing; checked binary parsing; synchronization; cache locality and profiling.
- **Stage 4 — Operating systems and asynchronous work.** Threads, Send/Sync, locks and memory ordering, bounded channels, Tokio I/O, cancellation, timeouts, resource cleanup and backpressure.
- **Stage 5 — Unsafe and system boundaries.** Invariants, raw pointers, foreign-function interfaces, platform ABIs, provenance, allocation contracts, alignment, and the limits of fuzzing/Miri. Require a safe implementation first.
- **Stage 6 — Production engineering.** APIs and databases where useful, tests, diagnostics, structured logs, profiling, robust deployment, and clear architectural trade-offs. Web frameworks are optional applications of the fundamentals, not the definition of low-level expertise.
- **Stage 7 — Mastery through independent delivery.** A complete tool built without AI-generated implementation, tested and benchmarked, with a team presentation and technical Q&A.

## Recommended independent company project: binary diagnostic-file inspector

Choose a binary input format with a short header, version, record count, and length-prefixed records. Write a CLI that reads and validates files, prints a human-readable summary, and returns precise errors. This is deliberately achievable before introducing databases, web frameworks or async runtimes.

**Milestone A: working program.** Create your own repository and CLI. Write a format specification and input fixtures. Parse one valid record, then multiple records.

**Milestone B: reliability.** Implement checked arithmetic, malformed-input handling, UTF-8 validation, tests for truncated and oversized fields, and clear exit statuses. Demonstrate no panics for adversarial input within your stated limits.

**Milestone C: systems mastery.** Compare buffered streaming to whole-file reading; profile release builds; examine allocations and memory layout; justify choices from measured evidence. As an extension, parse multiple independent files with a bounded worker pool.

**Milestone D: presentation.** Demonstrate valid input, corrupted input, tests, benchmark protocol, key ownership decisions, and one rejected design. Explain what the compiler prevented and what tests still had to catch.

**AI-free implementation protocol:** Keep an engineering journal. Source code and tests are written by the learner. Consult official documentation, compiler diagnostics, reference books, and human peers as permitted by company rules. AI may help plan the curriculum or review the *learning process* but should not generate implementation code, tests, or patches for this particular project.

## Skills rubric

| Level | Demonstrated capability |
| --- | --- |
| Beginner | Builds a CLI, uses Cargo, fixes compile errors, writes basic tests |
| Intermediate | Explains moves, borrows, lifetime relationships, errors and allocation choices |
| Advanced | Parses arbitrary binary input without unchecked offsets, handles I/O failures, evaluates threads and locking |
| Expert trajectory | Audits unsafe contracts, evaluates platform interfaces, reproduces performance findings, explains concurrency memory-order choices and limitations |

No single course certifies expertise. Repeated real systems work, code reviews, debugging and performance investigations are required.

## Source and verification policy

Use the maintained [Rust Book](https://doc.rust-lang.org/stable/book/), [Rust Reference](https://doc.rust-lang.org/reference/), [Cargo Book](https://doc.rust-lang.org/cargo/), [Rustonomicon](https://doc.rust-lang.org/nomicon/), [Rust API Guidelines](https://rust-lang.github.io/api-guidelines/), [Rust Performance Book](https://nnethercote.github.io/perf-book/), and [Miri](https://github.com/rust-lang/miri). Use Tokio and Axum documentation only where their features are taught. Treat version numbers as review stamps rather than assuming they remain current; the prior embedded stamp is Rust 1.97 / Edition 2024, reviewed 2026-08-18.

## Course structure (October 2026)

The systems path runs from installation to a capstone with a graded exercise in every lesson. Each code exercise has a fixture in `sandbox-broker/src/evaluator.mjs` and pass/fail reference solutions in `sandbox-broker/test/rust-solutions`, run by CI both with rustc and inside the sandbox image.

1. **Rust foundations / Project workflow:** setup; functions and formatting; number types and overflow; control flow and pattern matching; strings, UTF-8 and collections; Option/Result; errors; compiler errors, debugging and tests; modules and visibility; Cargo profiles, features and quality tools. Checkpoint: `rust-foundations` (12 questions).
2. **Memory-safe Rust:** stack, heap and layout (before ownership on purpose); ownership and Drop; borrowing; lifetimes; structs and enums; traits and generics; iterators and closures; smart pointers; macros.
3. **Working with the operating system:** arguments, environment and exit codes; stdio and buffering; files and system calls; processes and signals; serde; project: `minigrep`.
4. **Concurrency and systems:** threads, Send and Sync; async with Tokio; structured concurrency.
5. **Low-level foundations:** bits and byte order; slices and zero-copy buffers; raw pointers; unsafe, FFI and Miri; binary parser; atomics and locks; measuring performance and allocation.
6. **Networking from sockets up:** TCP with `std::net`; framing; concurrent servers with timeouts; project: Tokio key-value server.
7. **Systems practice:** property testing and fuzzing; targets, `no_std`, embedded and WebAssembly; **capstone: a crash-safe log-structured store.**

The service-delivery, quality/operations and staff modules that follow form the backend track and are still templated.

Grading conventions worth keeping:
- Network fixtures bind to 127.0.0.1 and start a watchdog thread so a hanging submission fails fast.
- Fixtures that need crates (Tokio, serde) compile with `pathway-rustc`; the image smoke test proves loopback networking works with no network.
- The raw-pointer and FFI graders require a `// SAFETY:` comment per `unsafe` block; the `no_std` grader compiles the submission as a `#![no_std]` library first.

### Known follow-up items

1. Service delivery, quality/operations and staff lessons are still templated one-liners with multiple-choice checks.
2. The frontend shows one checkpoint per course; module-end checkpoints for memory safety, OS, low-level and networking need the assessment UI (audit H1).
3. Production sandboxes use Vercel's `deny-all` policy; confirm loopback works there with one real submission to a networking lesson after deploy.
4. Optional deeper tracks: Linux interfaces (epoll, mmap), compiler/toolchain fundamentals, embedded hardware labs.
