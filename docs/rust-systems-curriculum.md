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

### Known follow-up items

1. Existing Rust lessons outside the expanded labs still have short concept text and immediate multiple-choice checks; upgrade systematically and move graded assessments to module ends.
2. Add automated checks for unique slugs, contiguous ordering, valid next-slug links, lesson coverage and compilable example snippets.
3. Develop deeper optional tracks for Linux interfaces, networking sockets, embedded/no_std, unsafe audit, C ABI interoperability, and compiler/toolchain fundamentals.
