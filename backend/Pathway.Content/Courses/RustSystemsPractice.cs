namespace Pathway.Content;

static partial class Curriculum
{
    // Authored lessons for the "Systems practice" module: property-based testing, compilation
    // targets and no_std, and the systems capstone (a crash-safe log-structured store).
    // Fixtures live in sandbox-broker/src/evaluator.mjs.

    private static Lesson RustTestingProperties(Lesson lesson) => lesson with
    {
        Subtitle = "Go beyond hand-picked examples: state rules that must hold for every input, check them against thousands of generated cases, and know where fuzzing, Miri and integration tests fit.",
        Concept = "An example test checks one input you thought of. A property test states a rule, such as \"decoding an encoded value returns the original\", and checks it against thousands of generated inputs, including ones you'd never think of. Systems code, which must survive arbitrary data, is tested with properties, fuzzing and Miri on top of ordinary unit and integration tests.",
        Body = """
            ## The limits of examples
            Every test so far has been an **example**: a specific input and the output you expect. Examples are essential, but they only cover the cases you imagined, and bugs live in the cases you didn't: the run of exactly 256 identical bytes, the empty input, the value at a boundary, the odd-length buffer. Systems code meets those cases constantly, because it processes data written by other programs, other machines and sometimes attackers.

            ## Properties
            A **property** is a statement that must be true for *every* valid input. Instead of choosing inputs, you write a generator that produces many of them, and check the property on each. Good properties come in a few recurring shapes:

            - **Round trip**: `decode(encode(x)) == x`, `parse(print(x)) == x`, `deserialize(serialize(x)) == x`. Any pair of inverse operations has one, and it catches an enormous range of bugs.
            - **Invariants of the output**: "encoding never more than doubles the size", "every count byte is at least 1", "the result is sorted", "the total is unchanged".
            - **Oracle**: compare a fast, clever implementation with a slow, obviously correct one on the same inputs.
            - **Idempotence**: doing it twice equals doing it once, like normalising a path or deduplicating.
            - **Never panics**: for any input whatsoever, the function returns, perhaps with an error.

            ## A worked example: run-length encoding
            **Run-length encoding** (RLE) compresses runs of repeated bytes into `(count, byte)` pairs: `aaab` becomes `[3, a, 1, b]`. It's simple, and it has a classic boundary: the count is one byte, so a run of 300 must be split into a pair of 255 and a pair of 45. An implementation that just keeps incrementing the count wraps 256 around to 0 in release builds, producing output that decodes to the wrong data. A few hand-written examples (`aaab`, `abc`, an empty string) all pass; the bug needs a run longer than 255, which nobody puts in a unit test until it bites. A property test with long generated runs finds it immediately.

            Decoding has its own failure modes. Encoded data from outside is untrusted: an odd number of bytes, or a count of zero, must be reported as errors, never cause a panic. "Decode never panics on arbitrary bytes" is a property too.

            ## Writing a generator
            A generator needs to be **deterministic**, so a failure can be reproduced: seed a simple pseudo-random number generator with a fixed value, and print the seed or case number when an assertion fails.

            Random data isn't automatically good test data. Uniformly random bytes almost never contain long runs, so they'd never trigger the RLE boundary. Good generators **bias toward structure**: a small alphabet (so repeats are common), varied lengths including zero and long ones, and occasional extreme values. Think about which inputs reach the interesting code, and make them likely.

            ## proptest and quickcheck
            In real projects, use a library. **proptest** provides strategies for generating values of any type and, crucially, **shrinking**: when a property fails on a large random input, it automatically searches for the *smallest* input that still fails, so instead of a 700-byte mess you get `[7; 256]`.

            ```rust
            use proptest::prelude::*;

            proptest! {
                #[test]
                fn round_trip(data in proptest::collection::vec(0u8..4, 0..700)) {
                    prop_assert_eq!(decode(&encode(&data)), Ok(data));
                }
            }
            ```

            It saves failing cases to a `proptest-regressions` file, so they're re-run forever after. Commit that file.

            ## Fuzzing: the same idea, pushed further
            The binary-parser lesson introduced **coverage-guided fuzzing** with `cargo fuzz`. A fuzzer is a property tester that watches which branches each input reaches and mutates inputs toward unexplored code, running millions of cases. Properties make fuzz targets stronger: a fuzz target can assert round trips and invariants, not only "doesn't crash". Use property tests in the normal test suite (fast, every commit) and fuzzing in longer runs (overnight, or continuously with a service like OSS-Fuzz).

            ## The layers of testing systems code
            A well-tested systems component usually has:

            - **Unit tests** for specific behaviour and every bug you've fixed, with examples that document intent.
            - **Property tests** for the rules that hold across all inputs.
            - **Fuzzing** for parsers, decoders and anything facing untrusted data.
            - **Miri** runs (the FFI lesson) for any module containing `unsafe`.
            - **Integration tests** in `tests/` that use the public API, or run the compiled binary as a user would, checking output, files and exit codes.
            - **Fault injection** for I/O: tests that simulate a full disk, a dropped connection, or a crash halfway through a write. The capstone at the end of this module does exactly that.

            ## Common mistakes
            - Only testing inputs you thought of, which are the ones your code already handles.
            - Unseeded randomness, so a failure can't be reproduced.
            - Generators that never produce the structure that matters (long runs, empty input, maximum sizes).
            - Properties so weak they can't fail, like "the output is a `Vec`".

            ## Try it locally
            1. Implement RLE with a wrapping counter and write five example tests that pass. Then add the round-trip property with long runs and watch it fail.
            2. Add `proptest` as a dev-dependency, rewrite the property with it, introduce a bug and read the shrunk counterexample.
            3. Write a `cargo fuzz` target for `decode` that asserts it never panics and that re-encoding its output decodes again to the same bytes.
            """,
        Example = """
            // Property: for any data, decoding the encoding returns the original.
            for case in 0..2_000 {
                let data = generator.bytes();
                assert_eq!(decode(&encode(&data)).as_deref(), Ok(&data[..]), "case {case}");
                assert!(encode(&data).len() <= data.len() * 2, "case {case}");
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Make run-length encoding pass its properties",
            "Implement run-length encoding and decoding. The grader checks a few examples, then 2,000 generated inputs against round-trip and size properties, then 2,000 random byte strings that `decode` must handle without panicking. Don't write a `main` function.",
            [
                "`fn encode(data: &[u8]) -> Vec<u8>` produces `(count, byte)` pairs for each run, splitting runs longer than 255 (`[7; 300]` encodes as `[255, 7, 45, 7]`)",
                "`fn decode(encoded: &[u8]) -> Result<Vec<u8>, DecodeError>` reverses it, with `#[derive(Debug, PartialEq)] enum DecodeError { OddLength, ZeroCount }` for an odd number of bytes or a pair whose count is 0",
                "`decode(&encode(x)) == Ok(x)` for every input, and `decode` never panics",
            ],
            """
            #[derive(Debug, PartialEq)]
            enum DecodeError {
                OddLength,
                ZeroCount,
            }

            fn encode(data: &[u8]) -> Vec<u8> {
                todo!()
            }

            fn decode(encoded: &[u8]) -> Result<Vec<u8>, DecodeError> {
                todo!()
            }
            """,
            null,
            [],
            "When encoding, stop extending a run when its count reaches 255 and start a new pair. When decoding, check `encoded.len() % 2` first, then walk `encoded.chunks_exact(2)`, rejecting a zero count, and extend the output with `std::iter::repeat_n(byte, count as usize)`.",
            ["Examples encode and decode correctly", "Round-trip and size properties hold for generated data", "Decoding arbitrary bytes never panics"]),
    };

    private static Lesson RustSystemsTargets(Lesson lesson) => lesson with
    {
        Title = "Targets: cross-compiling, no_std, embedded, and WebAssembly",
        Subtitle = "Build the same Rust for other operating systems and processors, write code that runs without an operating system, and choose where Rust fits from the constraints, not the trend.",
        Concept = "A target triple names the processor, vendor and operating system a binary is built for, and rustup can install the standard library for many of them. `std` assumes an operating system; `core` doesn't. A `#![no_std]` crate uses only `core` (and optionally `alloc`), which is what runs on microcontrollers and keeps libraries portable to WebAssembly and embedded devices.",
        Body = """
            ## Target triples
            When you run `cargo build`, Rust compiles for the machine you're on, the **host**. Its full name is a **target triple** such as:

            - `x86_64-unknown-linux-gnu`: 64-bit Intel/AMD, Linux, linked against the GNU C library.
            - `aarch64-apple-darwin`: Apple Silicon Macs.
            - `x86_64-pc-windows-msvc`: Windows with Microsoft's toolchain.
            - `aarch64-unknown-linux-musl`: 64-bit ARM Linux using musl, a small C library that makes fully **static** binaries easy (no shared libraries needed at runtime, handy for containers).
            - `thumbv7em-none-eabihf`: an ARM Cortex-M4F microcontroller with **no operating system** (`none`).
            - `wasm32-unknown-unknown` and `wasm32-wasip2`: WebAssembly, for browsers and for WASI runtimes.

            `rustc -vV` shows your host; `rustc --print target-list` lists all of them.

            ## Cross-compiling
            Building for a target other than the host is **cross-compiling**:

            ```sh
            rustup target add aarch64-unknown-linux-musl
            cargo build --release --target aarch64-unknown-linux-musl
            ```

            `rustup target add` downloads the precompiled standard library for that target. Compiling Rust code is then straightforward; **linking** is usually the hard part, because the final step needs a linker and C libraries for the target. Tools that handle that for you: **cross** (runs the build in a container that has the right toolchain), and **cargo-zigbuild** (uses Zig's cross-linker). CI systems commonly build release binaries for several targets this way.

            Code that differs per platform uses `cfg` (the Cargo lesson): `#[cfg(target_os = "linux")]`, `#[cfg(target_arch = "aarch64")]`, `#[cfg(target_pointer_width = "32")]`. Remember that `usize` is 32 bits on 32-bit targets, including WebAssembly's `wasm32`; code that assumes 64-bit sizes breaks there.

            ## std, alloc and core
            Rust's standard library is really three layers:

            - **`core`**: everything that needs no operating system and no heap: integers and their methods, `Option`, `Result`, iterators, slices, `str`, `char`, traits, atomics, `core::fmt`. It runs anywhere, including on bare hardware.
            - **`alloc`**: heap-allocated types, `Vec`, `String`, `Box`, `Rc`, `Arc`, `BTreeMap`. Needs a memory allocator but no operating system.
            - **`std`**: `core` and `alloc`, plus everything that needs an OS: files, threads, networking, time, environment variables, `HashMap` (its default hasher needs OS randomness), `println!`.

            A crate marked `#![no_std]` at the top of `lib.rs` links only `core`. It can opt into `alloc` with `extern crate alloc;` when it has an allocator. Libraries that only compute (parsers, encoders, checksums, data structures) are often written `no_std` so the same code runs in a server, a browser and a microcontroller. Check the "no_std" or "#![no_std]" mention on a crate's page before using it in embedded work.

            ## Fixed capacity: const generics
            Without a heap there's no `Vec`; collections have a capacity chosen at compile time. **Const generics** make that capacity part of the type:

            ```rust
            pub struct RingBuffer<const N: usize> {
                items: [u16; N],
                start: usize,
                len: usize,
            }

            impl<const N: usize> RingBuffer<N> {
                pub const fn new() -> Self {
                    RingBuffer { items: [0; N], start: 0, len: 0 }
                }
            }

            const EMPTY_READINGS: RingBuffer<64> = RingBuffer::new(); // built at compile time
            ```

            `RingBuffer<64>` and `RingBuffer<8>` are different types with different sizes, all on the stack or in static memory; nothing is allocated. A `const fn` can run at compile time, which is what lets `new()` initialise a `const` or `static`. A **ring buffer** (circular buffer) is the workhorse of embedded and real-time code: it keeps the most recent N items in a fixed array, overwriting the oldest when full, with an index that wraps around using `%`. The **heapless** crate provides production versions of `Vec`, `String` and queues with fixed capacity.

            Small targets also make overflow concrete. Sensor readings are often `u16`; averaging four readings of 60,000 needs a `u32` accumulator, because the sum (240,000) doesn't fit in 16 bits.

            ## Checksums
            Devices that send data over serial lines or radio need to detect corrupted bytes. A **checksum** is a small value computed from the data and sent along with it; the receiver recomputes it and compares. **Fletcher-16** is a classic lightweight one: keep two running sums modulo 255; the first adds each byte, the second adds the first sum after each byte, so the result depends on byte *order* as well as values. The final value is the second sum in the high byte and the first in the low byte. (For stronger detection, CRC-32 is standard; for protection against deliberate tampering, you need a cryptographic MAC, not a checksum.)

            ## Embedded Rust in brief
            A microcontroller program is `#![no_std]` and `#![no_main]`, provides a `#[panic_handler]` (there's no OS to print a message to), and talks to hardware through registers at fixed memory addresses. The ecosystem layers that safely:

            - **PAC** (peripheral access crate): generated from the chip's description, one safe function per register field.
            - **HAL** (hardware abstraction layer): drivers for pins, timers, serial, SPI and I2C, implementing the shared **embedded-hal** traits so device drivers work across chips.
            - Frameworks like **Embassy** (async on microcontrollers) and **RTIC** (interrupt-driven concurrency).
            - **probe-rs** flashes and debugs boards over a debug probe.

            *The Embedded Rust Book* walks through a first program on a cheap development board.

            ## WebAssembly
            WebAssembly (Wasm) is a portable instruction format run by browsers and standalone runtimes like Wasmtime. `wasm32-unknown-unknown` targets the browser, where **wasm-bindgen** and **wasm-pack** generate the JavaScript glue. `wasm32-wasip2` targets **WASI**, a system interface for files, clocks and sockets in a sandbox, used for plugins and edge computing. Rust is one of the strongest languages for Wasm because it has no garbage collector or large runtime to ship.

            ## Choosing deliberately
            Rust can target almost anything; that doesn't mean it should. Start from the constraints: memory and CPU budget, safety and security requirements, latency, the platform's existing ecosystem, the team's experience, and how long the code must live. Rust shines when correctness under untrusted input, predictable performance and low resource use all matter. A web front end, a data notebook or a mobile UI often has a better primary language, with Rust used for the one component where it pays off.

            ## Common mistakes
            - Assuming 64-bit `usize`.
            - Pulling `std` into a library that could be `no_std`, closing it off from embedded and Wasm users.
            - Overflowing a narrow accumulator when summing narrow values.
            - Treating a checksum as tamper protection.

            ## Try it locally
            1. Run `rustup target add wasm32-wasip2`, build a small program for it, and run it with `wasmtime`.
            2. Add `#![no_std]` to a library crate of yours and fix each error. Note which ones needed `alloc` and which needed redesign.
            3. Cross-compile a release binary for `x86_64-unknown-linux-musl` (or another target) with `cross`, and check it with `file`.
            """,
        Example = """
            #![no_std] // only `core`: runs without an operating system

            pub fn fletcher16(data: &[u8]) -> u16 {
                let (mut low, mut high) = (0u16, 0u16);
                for &byte in data {
                    low = (low + u16::from(byte)) % 255;
                    high = (high + low) % 255;
                }
                (high << 8) | low
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Write code that runs without an operating system",
            "Write a fixed-capacity ring buffer and a checksum using only `core`. The grader first compiles your code as a `#![no_std]` library (so `Vec`, `String`, `Box` and anything from `std` will fail), then tests it. Don't write a `main` function or a `#![no_std]` line yourself.",
            [
                "`pub struct RingBuffer<const N: usize>` holding `u16` readings in an array, with `pub const fn new() -> Self` and `pub fn len(&self) -> usize`",
                "`pub fn push(&mut self, value: u16) -> Option<u16>` adds a reading, returning the evicted oldest reading when the buffer was full; `pub fn average(&self) -> Option<u16>` returns the integer mean of the readings held (`None` when empty) without overflowing",
                "`pub fn fletcher16(data: &[u8]) -> u16`: two sums modulo 255, the first adding each byte and the second adding the first after each byte, returned as `(second << 8) | first` (`\"abcde\"` gives `0xC8F0`)",
            ],
            """
            pub struct RingBuffer<const N: usize> {
                items: [u16; N],
                start: usize,
                len: usize,
            }

            impl<const N: usize> RingBuffer<N> {
                pub const fn new() -> Self {
                    todo!()
                }

                pub fn push(&mut self, value: u16) -> Option<u16> {
                    todo!()
                }

                pub fn len(&self) -> usize {
                    todo!()
                }

                pub fn average(&self) -> Option<u16> {
                    todo!()
                }
            }

            pub fn fletcher16(data: &[u8]) -> u16 {
                todo!()
            }
            """,
            null,
            [],
            "The next free slot is `(start + len) % N`. When full, overwrite `items[start]`, return its old value, and advance `start` by one (wrapping). Sum readings into a `u32` before dividing. Fletcher-16 keeps two `u16` sums, applying `% 255` after each addition.",
            ["Compiles with only core", "The ring buffer evicts the oldest reading", "Average and checksum are exact without overflow"]),
    };

    private static Lesson RustSystemsCapstone(Lesson lesson) => lesson with
    {
        Subtitle = "Build the core of a log-structured key-value store: an append-only file format, an index rebuilt by replay, recovery from a torn final write, and atomic compaction. Then grow it into a project you can present.",
        Concept = "A log-structured store never overwrites data in place. Every change is appended to a log file, and an in-memory index of the latest value for each key is rebuilt by replaying the log at startup. A crash can only damage the last, partly written record, which recovery detects and cuts off. Compaction writes the live data to a new file and atomically renames it over the old log.",
        Body = """
            ## Why this project
            This capstone draws on nearly every module: file formats and byte order, `Read`/`Write` and buffering, the write-sync-rename pattern, checked parsing of untrusted bytes, error types, `HashMap`, and testing failure paths. It's also a real design: **Bitcask**, the storage engine behind the Riak database, works this way, and the log-structured idea underlies LevelDB, RocksDB, Kafka and the write-ahead logs of most databases.

            ## The design
            **The log.** A single file of records, appended to and never modified in place:

            ```text
            offset  size  field
            0       1     operation: 1 = SET, 2 = DELETE
            1       4     key length, u32 big-endian
            5       4     value length, u32 big-endian (0 for DELETE)
            9       k     key bytes (UTF-8)
            9+k     v     value bytes (UTF-8)
            ```

            So `SET k v` is exactly 11 bytes: `01 00000001 00000001 6B 76`.

            **The index.** A `HashMap<String, String>` holding the current value of every live key. `get` never touches the disk.

            **Writes.** `set` appends a SET record, then updates the index. `delete` appends a DELETE record (a **tombstone**), then removes the key from the index. Updating the index only *after* the append succeeded means a failed write never leaves the in-memory state claiming something the disk doesn't have.

            ```rust
            fn set(&mut self, key: &str, value: &str) -> io::Result<()> {
                self.file.write_all(&encode(SET, key, value))?; // one append...
                self.file.sync_data()?;                          // ...made durable...
                self.index.insert(key.to_string(), value.to_string()); // ...then remembered
                Ok(())
            }
            ```

            **Startup.** `open` reads the whole log and **replays** it from the beginning: each SET inserts, each DELETE removes. Because later records override earlier ones, replay ends in exactly the state the store was in. The log *is* the database; the index is a cache of it.

            Appending is fast (sequential writes are what disks and SSDs do best), and the design is simple to reason about. The costs: the log grows with every update even if the number of keys doesn't, and the whole index must fit in memory. Compaction addresses the first; the second is a real limit that more advanced designs (LSM trees, B-trees) remove.

            ## Durability: when is a write safe?
            The files lesson explained that `write` only puts data in the kernel's page cache. For a store that promises "once `set` returns, the data survives a crash", each append must be followed by `sync_data()` (`fdatasync`, which flushes the data without forcing every metadata update). That's slow: each sync waits for the device, often a millisecond or more.

            Real systems choose a point on the trade-off and document it:

            - Sync every write: safest, slowest.
            - **Group commit**: batch the writes that arrive within a few milliseconds and sync once for all of them.
            - Sync periodically (say every second): fast, but a crash can lose the last second of acknowledged writes.

            Being explicit about this choice, and testing it, is the difference between a toy and a storage engine.

            ## Crashes and torn writes
            If the power fails or the process is killed while a record is being appended, the file can end with a **torn write**: the first few bytes of a record and not the rest. Since records are only ever appended, a crash can damage only the *last* record.

            Recovery therefore treats the two kinds of bad data differently:

            - **A truncated final record** (the file ends inside a header, or before the key and value bytes it promised) is the expected result of a crash. Ignore it, and **cut it off** with `file.set_len(valid_length)`, so the next append starts on a clean boundary. If you skip the truncation, the next record is written *after* the garbage, and the next replay stops at the garbage and loses everything after it, including writes that were acknowledged.
            - **Anything else malformed**, such as an unknown operation byte or invalid UTF-8 in a complete record, is **corruption**: the file was damaged or written by something else. Refuse to open it with an `InvalidData` error. Silently skipping corrupt records would quietly lose or misinterpret data.

            Production logs add a **checksum** (usually CRC-32) to every record, so a bit flip in the middle of the file is detected too, and so a torn record that happens to look complete is still caught. That's the first extension below.

            ## Compaction
            After a million updates to one key, the log holds a million records and the index holds one. **Compaction** rewrites the log with just the live data:

            1. Write a SET record for every live key to a new file, `data.tmp`, next to the log.
            2. `sync_all` it.
            3. `rename` it over the log, which is atomic within one filesystem.
            4. Reopen the append handle on the new file.

            This is the write-sync-rename pattern from the files lesson. At every instant, the path names either the complete old log or the complete new one, so a crash during compaction loses nothing. Writing the keys in sorted order makes the output deterministic, which makes compaction testable.

            Note the open file handle: after the rename, the old handle still refers to the *old* file (now unnamed). Appends through it would vanish when it's closed. That's why step 4 exists.

            ## Testing what can go wrong
            The grader for this lesson, like any serious test suite for storage, deliberately damages files: it appends half a record to simulate a crash, writes a record with an unknown operation, compacts and reopens, and checks the exact bytes of the format. Do the same in your own tests, and add a **crash loop**: a script that starts a process writing records, kills it with `SIGKILL` at a random moment, reopens the store, and checks that every acknowledged write is present. Repeat a thousand times.

            ## Growing it into your project
            The graded part is the core engine. To make it a project you can present and defend, extend it on your own:

            1. **Checksums**: add a CRC-32 field to each record (the `crc32fast` crate), and test that a flipped bit anywhere is detected.
            2. **A network front end**: put the store behind the Tokio key-value server from the networking module. Decide how concurrent connections share it (a `Mutex`, or a single writer task receiving commands over a channel), and keep the protocol's line-length and connection limits.
            3. **Group commit**, with a measured comparison of sync-every-write against batching: throughput, latency, and what a crash can lose in each mode.
            4. **Automatic compaction** when the log grows past a multiple of the live data, without blocking readers for long.
            5. **Crash testing**: the kill loop above, run in CI.
            6. **A design document**: the format, the durability promise, the recovery rules, what you measured, and one design you rejected and why. Then present it: walk someone through a crash during compaction and explain why no data is lost.

            Build it yourself, using the documentation and your earlier exercises; the struggle is where the understanding comes from.

            ## Common mistakes
            - Updating the index before the write reaches the log.
            - Treating a torn tail as corruption (the store won't open after a crash) or skipping it without truncating (later writes are lost).
            - Compacting in place by truncating and rewriting the log.
            - Forgetting to reopen the append handle after compaction.
            - Claiming durability without a sync, or syncing without measuring the cost.
            """,
        Example = """
            fn compact(&mut self) -> io::Result<()> {
                let temporary = self.path.with_extension("tmp");
                let mut output = File::create(&temporary)?;
                for (key, value) in &self.index {
                    output.write_all(&encode(SET, key, value))?;
                }
                output.sync_all()?;
                drop(output);
                fs::rename(&temporary, &self.path)?;                // atomic swap
                self.file = OpenOptions::new().append(true).open(&self.path)?; // new file
                Ok(())
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Build a crash-safe log-structured store",
            "Implement the store described in the lesson. The grader checks the exact byte format, persistence across reopen, recovery from a torn final record, compaction, and rejection of corrupt records, all on real files in a temporary folder. Add any helpers you need. Don't write a `main` function.",
            [
                "`struct Store` with `fn open(path: &Path) -> io::Result<Store>` (creating the file if needed and replaying it), `fn set(&mut self, key: &str, value: &str) -> io::Result<()>`, `fn delete(&mut self, key: &str) -> io::Result<bool>` (true if the key existed), `fn get(&self, key: &str) -> Option<&str>` and `fn len(&self) -> usize`",
                "Records use the format in the lesson exactly: op byte (1 SET, 2 DELETE), key length and value length as big-endian `u32`, then key and value bytes",
                "A partly written final record is ignored and cut off the file so later writes can be read back; any other malformed record makes `open` fail with `InvalidData`",
                "`fn compact(&mut self) -> io::Result<()>` rewrites only live keys through a temporary file and an atomic rename, leaves no temporary file behind, and the store keeps working afterwards",
                "Every write is durable before `set` or `delete` returns",
            ],
            """
            use std::collections::HashMap;
            use std::fs::{self, File, OpenOptions};
            use std::io::{self, Read, Write};
            use std::path::{Path, PathBuf};

            struct Store {
                path: PathBuf,
                file: File,
                index: HashMap<String, String>,
            }

            impl Store {
                fn open(path: &Path) -> io::Result<Store> {
                    todo!()
                }

                fn set(&mut self, key: &str, value: &str) -> io::Result<()> {
                    todo!()
                }

                fn delete(&mut self, key: &str) -> io::Result<bool> {
                    todo!()
                }

                fn get(&self, key: &str) -> Option<&str> {
                    todo!()
                }

                fn len(&self) -> usize {
                    todo!()
                }

                fn compact(&mut self) -> io::Result<()> {
                    todo!()
                }
            }
            """,
            null,
            [],
            "Open with `OpenOptions::new().read(true).append(true).create(true)`, `read_to_end`, and replay with a loop that stops (without error) when the remaining bytes are too short for a header or a body, returning the length of the valid prefix. If that's shorter than the file, `set_len` it. Write each record with one `write_all` followed by `sync_data`. For compaction, follow the lesson's four steps, then replace `self.file`.",
            ["The record format is exact", "State survives a reopen", "A torn final record is recovered", "Compaction is atomic and the store keeps working", "Corrupt records are rejected"]),
    };
}
