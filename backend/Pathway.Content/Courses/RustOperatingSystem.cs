namespace Pathway.Content;

static partial class Curriculum
{
    // Authored lessons for the "Working with the operating system" module: arguments and exit
    // codes, standard streams, files, processes, serialization, and a grep-style project.
    // Fixtures live in sandbox-broker/src/evaluator.mjs.

    private static Lesson RustCliArgsEnv(Lesson lesson) => lesson with
    {
        Subtitle = "Read what the operating system hands a program at startup, parse it the way users expect, and report the outcome with a meaningful exit code.",
        Concept = "When a program starts, the process that launched it supplies a list of arguments, a set of environment variables, a working directory and three open streams. When it finishes, it returns a small number, the exit code, where 0 means success. A command-line tool is mostly a careful translation between those conventions and its own logic.",
        Body = """
            ## What a process starts with
            Every program on Linux, macOS or Windows is started by another process: a shell, a service manager, an IDE, a CI runner. That parent decides what the new process receives:

            - **Arguments**: a list of strings. By convention the first is the program's own name or path.
            - **Environment variables**: a set of `NAME=value` pairs, inherited from the parent unless it changes them. `PATH`, `HOME`, `LANG` and `TERM` are typical.
            - **A working directory**: where relative paths like `notes.txt` are looked up.
            - **Three open streams**: standard input, output and error (next lesson).

            When the program ends, it gives back one small integer, the **exit code** or exit status. The parent reads it to decide what to do next: a shell's `&&` runs the next command only if the exit code was 0, and CI marks a step failed on any non-zero code.

            ## Arguments in Rust
            ```rust
            let args: Vec<String> = std::env::args().skip(1).collect();
            ```

            `std::env::args()` returns an iterator over the arguments; `.skip(1)` drops the program name. Two details matter in real tools:

            - The shell has already split the command line and removed quotes. `grep "two words" file` arrives as two arguments, `two words` and `file`. Your program never sees the quotes.
            - Arguments aren't guaranteed to be valid UTF-8 (a file name on Linux can be any bytes). `env::args()` panics on invalid UTF-8; `env::args_os()` returns `OsString`s that can hold anything. Tools that pass file names straight through to the file system should prefer `args_os`.

            ## Conventions users expect
            Command-line tools share conventions, mostly from POSIX and GNU, and following them is part of a tool being usable:

            - **Short options** start with one dash (`-i`), **long options** with two (`--ignore-case`). Many tools accept both for the same setting.
            - An option can take a **value** from the next argument: `-m 3`, `--max-count 3`. Many tools also accept `--max-count=3`.
            - Options and plain arguments can usually appear in any order: `grep todo -i notes.md` works like `grep -i todo notes.md`.
            - A bare **`--`** means "no more options". Everything after it is a plain argument, even if it starts with a dash. It's how you search for the text `-i`: `grep -- -i file`.
            - A lone **`-`** usually means standard input instead of a file.
            - Unknown options are an error, reported with the option's name, not silently ignored.

            Writing the parser yourself once is the best way to understand these rules. Keep it a **pure function** over a slice of strings, `fn parse_args(args: &[String]) -> Result<Config, String>`, rather than reading `env::args()` inside it. Then tests can call it with any arguments, and `main` is the only place that touches the real process.

            For production tools, use the **clap** crate. With its derive API you describe the options as a struct, and it generates parsing, `--help` text, error messages and shell completions:

            ```rust
            #[derive(clap::Parser)]
            struct Cli {
                /// Text to search for
                pattern: String,
                /// Files to search (standard input if none)
                paths: Vec<std::path::PathBuf>,
                #[arg(short, long)]
                ignore_case: bool,
            }
            ```

            ## Environment variables
            `std::env::var("HOME")` returns `Result<String, VarError>`: an error if the variable is missing, or if its value isn't valid UTF-8. It's a `Result` because absence is normal, and you decide what it means.

            Environment variables suit settings that belong to the *environment* rather than one invocation: credentials in CI, a log level, where a service should listen. Two widely respected conventions:

            - **`NO_COLOR`**: if it's set to a non-empty value, don't print coloured output.
            - **`TERM=dumb`**: the terminal can't handle colours or cursor movement.

            Colour also makes no sense when output goes to a file or another program; `std::io::stdout().is_terminal()` (from the `IsTerminal` trait) tells you whether a person is watching.

            When a setting can come from several places, the usual precedence, from strongest to weakest, is: **command-line flag, then environment variable, then config file, then built-in default.** The most specific, most recent instruction wins.

            ## Exit codes
            The meaning of 0 is universal: success. Beyond that, conventions vary by tool, but these are common:

            - `0`: success. For a search tool, "found something".
            - `1`: the tool ran correctly but the answer is "no" (grep found no matches, a test suite had failures).
            - `2`: misuse or error: bad arguments, an unreadable file.
            - `126` and `127` are set by shells: "found but not executable" and "command not found".
            - `128 + N`: the process was killed by signal N, as reported by shells. `130` is Ctrl-C (signal 2).

            Exit codes range from 0 to 255 on Unix; larger values wrap around, so never use one as a count.

            In Rust, the clean way to exit with a code is to return it from `main`:

            ```rust
            use std::process::ExitCode;

            fn main() -> ExitCode {
                let args: Vec<String> = std::env::args().skip(1).collect();
                match run(&args) {
                    Ok(0) => ExitCode::from(1),
                    Ok(_) => ExitCode::SUCCESS,
                    Err(message) => {
                        eprintln!("mytool: {message}");
                        ExitCode::from(2)
                    }
                }
            }
            ```

            `std::process::exit(code)` also works, but it ends the process **immediately, without running destructors**: buffered output that hasn't been flushed is lost, and temporary files aren't cleaned up. Returning from `main` unwinds normally first. `main` can also return `Result<(), E>`; an `Err` prints its Debug form and exits with code 1.

            ## Common mistakes
            - Parsing arguments by position only, so `-i` must come first and `--` isn't supported.
            - Printing an error but still exiting with 0, so scripts and CI think the tool succeeded.
            - Calling `process::exit` with output still sitting in a buffer.
            - Reading environment variables deep inside library code, which makes behaviour depend on invisible state and hard to test. Read them once in `main`.

            ## Try it locally
            1. Write a `main` that prints each argument on its own line, wrapped in brackets. Run it as `cargo run -- "two words" -x --`. Note that `cargo run --` passes everything after `--` to your program.
            2. After running it from a shell, print the exit code with `echo $?` (macOS/Linux) or `$LASTEXITCODE` (PowerShell).
            3. Run `NO_COLOR=1 cargo run` and read the variable with `env::var`.
            """,
        Example = """
            use std::process::ExitCode;

            fn main() -> ExitCode {
                let args: Vec<String> = std::env::args().skip(1).collect();
                let Some(pattern) = args.first() else {
                    eprintln!("usage: find-text PATTERN [FILE...]");
                    return ExitCode::from(2);
                };
                let colour = std::env::var("NO_COLOR").map_or(true, |value| value.is_empty());
                println!("searching for {pattern} (colour: {colour})");
                ExitCode::SUCCESS
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Parse arguments like a Unix tool",
            "Write an argument parser that follows the conventions in the lesson, plus two small functions for colour and exit codes. Everything is a pure function, so the grader can call it with any inputs. Don't write a `main` function.",
            [
                "`#[derive(Debug, PartialEq)] struct Config { pattern: String, paths: Vec<String>, ignore_case: bool, max_count: Option<usize> }`",
                "`fn parse_args(args: &[String]) -> Result<Config, String>` (no program name): `-i`/`--ignore-case`; `-m N`/`--max-count N`; options may appear anywhere; after `--` everything is plain; a lone `-` is plain; the first plain argument is the pattern and the rest are paths. Errors, exactly: `missing pattern`, `missing value for <option>`, `invalid number for <option>: <value>`, `unknown option: <option>`",
                "`fn color_enabled(no_color: Option<&str>, term: Option<&str>) -> bool` is false when `NO_COLOR` is set to a non-empty value or `TERM` is `dumb`; `fn exit_code(result: &Result<usize, String>) -> i32` returns 0 for one or more matches, 1 for none, and 2 for an error",
            ],
            """
            #[derive(Debug, PartialEq)]
            struct Config {
                pattern: String,
                paths: Vec<String>,
                ignore_case: bool,
                max_count: Option<usize>,
            }

            fn parse_args(args: &[String]) -> Result<Config, String> {
                todo!()
            }

            fn color_enabled(no_color: Option<&str>, term: Option<&str>) -> bool {
                todo!()
            }

            fn exit_code(result: &Result<usize, String>) -> i32 {
                todo!()
            }
            """,
            null,
            [],
            "Walk the arguments with `let mut iter = args.iter(); while let Some(arg) = iter.next()`, so an option like `-m` can call `iter.next()` to take its value. Keep a flag for having seen `--`, and treat anything that doesn't start with `-` (or is exactly `-`) as plain. Collect plain arguments, then take the first as the pattern.",
            ["Options and plain arguments are told apart correctly", "Bad input gives precise errors", "Colour and exit codes follow common conventions"]),
    };

    private static Lesson RustStdioBuffering(Lesson lesson) => lesson with
    {
        Subtitle = "Read input and write output through the `Read` and `Write` traits, buffer for speed, and handle the errors that real pipes produce.",
        Concept = "Standard input, output and error are three file descriptors every process starts with. Rust reads and writes them, and files, sockets and in-memory buffers, through the same `Read` and `Write` traits. Buffering turns many tiny reads or writes into a few large system calls, and every write can fail, for example when the program reading your output exits.",
        Body = """
            ## Three streams
            Every process starts with three open **file descriptors**: small integers the operating system uses to identify an open file, pipe, terminal or socket.

            - **0, standard input (stdin)**: where the program reads input.
            - **1, standard output (stdout)**: where results go.
            - **2, standard error (stderr)**: where diagnostics, warnings and progress messages go.

            The parent decides what each is connected to. In a terminal, all three are the terminal. With `tool < input.txt > out.txt`, stdin is a file and stdout is another. In a pipeline, `cat log | tool | sort`, the shell connects one program's stdout to the next one's stdin with a **pipe**, a small kernel buffer that one side writes into and the other reads from.

            Keeping results on stdout and messages on stderr is what makes tools composable: `tool | sort` sorts only the results, while errors still reach the person at the terminal. In Rust, `println!` writes to stdout and `eprintln!` to stderr.

            ## Read and Write
            Rust's I/O is built on two traits in `std::io`:

            - **`Read`**: `read(&mut buf) -> io::Result<usize>` fills some of a buffer and returns how many bytes it read; `0` means end of input.
            - **`Write`**: `write(&buf) -> io::Result<usize>` writes *some* of the bytes and returns how many; `flush()` pushes buffered data onward.

            `File`, `TcpStream`, `Stdin`, `Stdout`, `Vec<u8>` (for writing) and `std::io::Cursor` (for reading from memory) all implement them. That's the key to testable I/O: write functions that take *any* reader or writer, and tests can pass in-memory data while `main` passes the real streams.

            ```rust
            use std::io::{self, BufRead, Write};

            fn number_lines<R: BufRead, W: Write>(reader: R, mut out: W) -> io::Result<()> {
                for (index, line) in reader.lines().enumerate() {
                    writeln!(out, "{:>4}  {}", index + 1, line?)?;
                }
                out.flush()
            }
            ```

            `writeln!` is `println!` for any `Write`, and it returns an `io::Result` you must handle; `?` passes it up.

            Note a subtlety with `read` and `write`: a single call may handle **fewer bytes than you asked for**. That's not an error; it's how pipes, sockets and the kernel work. Use `read_exact` and `write_all`, which loop until everything is done, unless you're deliberately handling partial transfers yourself.

            ## Buffering
            Each `read` or `write` on a raw stream is a **system call**: a request to the operating system kernel, which costs far more than an ordinary function call. Writing a 10-byte line with its own system call, a million times, is dramatically slower than collecting those lines in memory and writing them 8 KB at a time. That's **buffering**:

            - **`BufReader<R>`** reads a large chunk at once and hands it out in pieces. It also implements **`BufRead`**, which adds line-oriented methods: `read_line`, `lines()`, `read_until`, `split`.
            - **`BufWriter<W>`** collects writes in memory and sends them in large chunks.

            Rust's own `Stdout` is **line-buffered**: it flushes at every newline. That's friendly in a terminal, and a bottleneck when writing a million lines to a file. For bulk output, lock stdout once and wrap it:

            ```rust
            let stdout = io::stdout();
            let mut out = io::BufWriter::new(stdout.lock());
            for item in results {
                writeln!(out, "{item}")?;
            }
            out.flush()?;
            ```

            `lock()` also avoids re-acquiring the stdout lock on every write; `println!` does that each time.

            **Always call `flush()` yourself at the end.** A `BufWriter` flushes when dropped, but it can't report an error from inside `drop`, so a failed final write would vanish silently.

            ## Reading lines correctly
            `io::stdin().lock().lines()` gives an iterator of `io::Result<String>`, one per line, with the line ending removed. Convenient, but notice what's lost: the newline bytes, and therefore exact byte counts. When you need the raw lines, `read_line` appends a line **including** its newline to a `String` you reuse, and returns how many bytes it read:

            ```rust
            let mut line = String::new();
            loop {
                line.clear();                  // read_line appends, so clear first
                let bytes = reader.read_line(&mut line)?;
                if bytes == 0 { break; }       // end of input
                // `line` still ends with '\n' (unless it was the last line)
            }
            ```

            Reusing one `String` avoids an allocation per line, which matters on big inputs. The last line of a file may not end with a newline; code that counts lines by counting `'\n'` characters disagrees with code that counts lines read, and you should decide deliberately which you mean.

            Both `lines()` and `read_line` require valid UTF-8 and return an `io::Error` of kind **`InvalidData`** when they meet anything else. For input that may not be text, read bytes with `read_until(b'\n', &mut Vec<u8>)` and decide what to do with invalid sequences, for example `String::from_utf8_lossy`, which substitutes a replacement character.

            ## When writing fails: broken pipes
            Run `yes | head -n 1`: `head` reads one line and exits, closing its end of the pipe. The next write by `yes` has nowhere to go. On Unix, that write fails with **`BrokenPipe`**. (C programs are normally killed by the SIGPIPE signal at that point; Rust programs ignore that signal and get the error instead.)

            This has a practical consequence: `println!` **panics** if writing to stdout fails. A Rust tool using `println!` that is piped into `head` can crash with "failed printing to stdout". Robust tools write with `writeln!` and `?`, and when the error kind is `BrokenPipe`, exit quietly with success, since the reader simply stopped listening.

            Ignoring write errors with `let _ = writeln!(...)` is worse: the program carries on producing output nobody receives, and a full disk looks like success.

            ## Common mistakes
            - `println!` in a hot loop: one lock and one flush per line.
            - Forgetting `line.clear()` before `read_line`, so lines pile up in the same string.
            - Counting bytes with `lines()`, which drops the line endings.
            - Discarding write errors, or relying on `BufWriter`'s drop to flush.
            - Mixing results and diagnostics on stdout, so piping the output breaks.

            ## Try it locally
            1. Write a word counter that reads stdin, then run `cargo run < src/main.rs` and `cat src/main.rs | cargo run`.
            2. Print a million numbers with `println!`, then with a locked `BufWriter`, and time both with `cargo run --release > /dev/null`.
            3. Pipe a program that prints forever into `head -n 3` and see the `println!` panic. Then switch to `writeln!` and handle `BrokenPipe`.
            """,
        Example = """
            use std::io::{self, BufRead, BufWriter, Write};

            fn main() -> io::Result<()> {
                let stdin = io::stdin().lock();
                let mut out = BufWriter::new(io::stdout().lock());
                for (index, line) in stdin.lines().enumerate() {
                    writeln!(out, "{:>4}  {}", index + 1, line?)?;
                }
                out.flush() // report the last write's error instead of losing it in drop
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Count and number lines from any stream",
            "Write two functions that work with any reader and writer: a `wc`-style counter and a line numberer. The grader passes in-memory input, invalid UTF-8, and a writer that fails like a closed pipe. Don't write a `main` function.",
            [
                "`fn count<R: BufRead>(reader: R) -> io::Result<(usize, usize, usize)>` returns (lines, words, bytes); bytes include line endings, a final line without a newline still counts, and invalid UTF-8 returns an `InvalidData` error",
                "`fn number_lines<R: BufRead, W: Write>(reader: R, out: W) -> io::Result<()>` writes each line as its number right-aligned to width 4, two spaces, the line and a newline (`   1  alpha`)",
                "Every write error from `out` is returned to the caller, never ignored",
            ],
            """
            use std::io::{self, BufRead, Write};

            fn count<R: BufRead>(reader: R) -> io::Result<(usize, usize, usize)> {
                todo!()
            }

            fn number_lines<R: BufRead, W: Write>(reader: R, out: W) -> io::Result<()> {
                todo!()
            }
            """,
            null,
            [],
            "For `count`, take `mut reader`, reuse one `String`, and loop on `reader.read_line(&mut line)?`: it returns the bytes read (0 at the end) and keeps the newline, so add that number to the byte count. Count words with `split_whitespace().count()`. For `number_lines`, `writeln!(out, \"{:>4}  {}\", n, line)?` and finish with `out.flush()`.",
            ["Lines, words and bytes are counted exactly", "Lines are numbered in the required format", "Read and write errors reach the caller"]),
    };

    private static Lesson RustOsFilesSyscalls(Lesson lesson) => lesson with
    {
        Title = "Files, file descriptors, and system calls",
        Subtitle = "Understand what really happens when Rust opens, writes and renames a file, and write files that survive crashes and report every failure.",
        Concept = "A program can't touch the disk itself; it asks the kernel through system calls such as open, read, write, fsync and rename. A `File` owns a file descriptor and closes it when dropped. Writes land in the kernel's memory first and reach the disk later, so durable, crash-safe updates are written to a temporary file, flushed, then renamed over the original.",
        Body = """
            ## User space and the kernel
            Your program runs in **user space**, a restricted mode where it can compute and use its own memory but cannot directly touch hardware: disks, network cards, other processes' memory. The operating system **kernel** runs with full privileges and does those things on your program's behalf. The request is a **system call**: the program asks the kernel to do something, the processor switches into kernel mode, the kernel checks permissions and does the work, and control returns with a result or an error code.

            The file-related system calls on Linux and macOS (Windows has close equivalents):

            - `open`: find a file by path, check permissions, and return a **file descriptor**, a small integer indexing the process's table of open files.
            - `read` / `write`: transfer bytes between a buffer in your memory and the open file, starting at the file's current position.
            - `close`: release the descriptor.
            - `fsync`: wait until the file's data has physically reached the storage device.
            - `rename`: give a file a new name, replacing whatever had that name.

            Rust's `std::fs` is a safe, portable layer over these. On Linux, `strace -e trace=file,read,write ./your_program` prints every system call a program makes, which is the most direct way to see what your code actually asks the kernel to do.

            ## File owns a descriptor
            `std::fs::File` is a struct holding a file descriptor. It's an owned resource, so the ownership lesson applies directly: when the `File` is dropped, its `Drop` implementation closes the descriptor. You can't forget to close a file, and you can't use one after it's closed. (Processes have a limit on open descriptors, often 1024, and leaking them is a classic bug in long-running servers.)

            Quick helpers cover most needs:

            - `fs::read_to_string(path)` and `fs::read(path)` read a whole file.
            - `fs::write(path, contents)` creates or truncates a file and writes it.
            - `fs::create_dir_all`, `fs::remove_file`, `fs::read_dir`, `fs::metadata` (size, timestamps, whether it's a directory).

            For control over *how* a file is opened, use `OpenOptions`, which maps directly onto the flags of the `open` system call:

            ```rust
            use std::fs::OpenOptions;
            use std::io::Write;

            let mut log = OpenOptions::new()
                .create(true)   // make it if it doesn't exist
                .append(true)   // every write goes to the end
                .open("events.log")?;
            writeln!(log, "started")?;
            ```

            - `.append(true)`: each write goes to the current end of the file, even if another process appended in between. Without it, opening with `.write(true)` starts at position 0 and overwrites existing bytes.
            - `.truncate(true)`: empty the file on open. `File::create` is `write + create + truncate`.
            - `.create_new(true)`: fail with `AlreadyExists` if the file exists, an atomic way to claim a name (useful for lock files).

            Paths are `&Path` and `PathBuf`, not strings, because operating systems allow file names that aren't valid UTF-8. Join paths with `path.join("name")` rather than formatting strings with `/`, so the code works on Windows too.

            ## Errors you must expect
            Every file operation can fail, and `io::Error::kind()` tells you how:

            - `NotFound`: no such file or directory.
            - `PermissionDenied`: the process isn't allowed.
            - `AlreadyExists`: from `create_new`.
            - `InvalidData`: for example, invalid UTF-8 in `read_to_string`.
            - Others: disk full, too many open files, interrupted operations.

            A good rule: **return the error** unless you have a real, specific recovery. Hiding `NotFound` behind `unwrap_or_default()` turns "the config file is missing" into "the config is empty", which surfaces later as a baffling bug far from the cause.

            ## The page cache, and why "written" isn't "saved"
            When `write` returns successfully, your bytes have been copied into the kernel's **page cache** in memory. The kernel writes them to disk later, typically within seconds, in the background. That makes writes fast, but it means a power loss or kernel crash shortly after a "successful" write can lose the data.

            `file.sync_all()` calls `fsync`: it blocks until the data and the file's metadata are on the device. It's slow, measured in milliseconds, so it's for data that must not be lost (a database commit, a saved document), not for every log line.

            ## Replacing a file safely
            Suppose a program saves its settings by truncating `settings.json` and writing the new contents. If it crashes, or the disk fills, halfway through, the file is left half-written, and the old settings are gone too. The standard fix uses the fact that **`rename` within one filesystem is atomic**: anyone opening the file sees either the old one or the new one, never a mixture.

            ```rust
            use std::fs::{self, File};
            use std::io::Write;
            use std::path::Path;

            fn write_atomically(path: &Path, contents: &str) -> std::io::Result<()> {
                let temporary = path.with_extension("tmp");     // same directory, same filesystem
                let mut file = File::create(&temporary)?;
                file.write_all(contents.as_bytes())?;
                file.sync_all()?;                               // the new bytes are on disk
                drop(file);
                fs::rename(&temporary, path)                    // atomic swap
            }
            ```

            Each step matters: the temporary file must be in the same directory (rename can't be atomic across filesystems), it must be fully written and synced before the rename (or the rename can become durable before the data does), and the original is never opened for writing. Fully durable code also syncs the *directory* after the rename, so the new name itself survives a crash. Editors, package managers and databases all use this pattern.

            ## Reading large files
            `read_to_string` loads everything into memory, which is fine for configuration and wrong for a 20 GB log. For large files, stream through a `BufReader`, keeping only what you need. "The last N lines" of a huge file needs only N lines in memory at a time, held in a `VecDeque` where you push to the back and pop from the front.

            ## Common mistakes
            - Overwriting important files in place instead of write-sync-rename.
            - Opening a log without `.append(true)`, so each run overwrites it.
            - Treating every error as "file missing" or hiding errors behind defaults.
            - Building paths with string formatting and `/`.
            - Calling `sync_all` after every small write in a hot loop.

            ## Try it locally
            1. On Linux, run a small program that reads one file and writes another under `strace -f -e trace=openat,read,write,close,rename,fsync`. Match each line of output to a line of your code.
            2. Write a file with `File::create`, then kill the program before it finishes writing a large buffer, and look at the half-written result. Repeat with `write_atomically`.
            3. Run `ulimit -n 64`, then open files in a loop without dropping them, and read the error kind you get.
            """,
        Example = """
            use std::fs::OpenOptions;
            use std::io::{self, Write};
            use std::path::Path;

            fn append_line(path: &Path, line: &str) -> io::Result<()> {
                let mut file = OpenOptions::new().create(true).append(true).open(path)?;
                writeln!(file, "{line}")
            } // the file descriptor is closed here, when `file` is dropped
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Append, tail, and replace files safely",
            "Write three file functions. The grader runs them in a temporary folder, checks file contents and leftovers, and checks the error kind for a missing file. Don't write a `main` function.",
            [
                "`fn append_line(path: &Path, line: &str) -> io::Result<()>` creates the file if needed and adds the line plus `\\n` to the end, keeping what was already there",
                "`fn tail(path: &Path, n: usize) -> io::Result<Vec<String>>` returns the last `n` lines in order (all of them if there are fewer); a missing file returns the `NotFound` error",
                "`fn write_atomically(path: &Path, contents: &str) -> io::Result<()>` replaces the file's contents by writing a temporary file in the same folder, syncing it, and renaming it over the original, leaving no temporary file behind",
            ],
            """
            use std::fs::{self, File, OpenOptions};
            use std::io::{self, BufRead, BufReader, Write};
            use std::path::Path;

            fn append_line(path: &Path, line: &str) -> io::Result<()> {
                todo!()
            }

            fn tail(path: &Path, n: usize) -> io::Result<Vec<String>> {
                todo!()
            }

            fn write_atomically(path: &Path, contents: &str) -> io::Result<()> {
                todo!()
            }
            """,
            null,
            [],
            "`OpenOptions::new().create(true).append(true).open(path)?` for appending. For `tail`, open with `File::open(path)?` (the `?` passes NotFound along), read lines with a `BufReader`, and keep at most `n` in a `VecDeque`. For the atomic write, use `path.with_extension(\"tmp\")`, `write_all`, `sync_all`, then `fs::rename(&temporary, path)`.",
            ["Appends keep earlier lines", "Tail returns the last lines and reports missing files", "Replacement is atomic and leaves nothing behind"]),
    };

    private static Lesson RustProcessesSignals(Lesson lesson) => lesson with
    {
        Title = "Processes, pipes, exit status, and signals",
        Subtitle = "Start other programs safely, connect their input and output, read how they ended, and understand the signals that stop processes.",
        Concept = "`std::process::Command` starts another program with an explicit list of arguments, never through a shell, so data can't turn into commands. You choose what its standard streams connect to, wait for it, and read its exit status, which is either an exit code or, on Unix, the signal that killed it.",
        Body = """
            ## What a process is
            A **process** is a running program together with everything the kernel tracks for it: its own private memory, its table of open file descriptors, its environment and working directory, the user it runs as, and a **process ID** (PID). Processes are isolated: one can't read another's memory. They cooperate through files, pipes, sockets, signals and exit codes.

            On Unix, starting a program is classically two steps: `fork` duplicates the current process, and `exec` replaces the duplicate's program with a new one. Between the two, the child can rearrange its file descriptors, which is how shells set up `>` redirections and `|` pipes. Rust's `Command` does all of this for you (using faster equivalents like `posix_spawn` where it can) behind a portable builder.

            ## Command
            ```rust
            use std::process::Command;

            let output = Command::new("git")
                .args(["log", "--oneline", "-n", "5"])
                .current_dir("/path/to/repo")
                .output()?;
            println!("{}", String::from_utf8_lossy(&output.stdout));
            ```

            Three ways to run it, depending on what you need:

            - **`.status()`**: run it with the parent's stdin, stdout and stderr, wait, and return its `ExitStatus`. Good for "run this and let its output go to the terminal".
            - **`.output()`**: capture stdout and stderr into `Vec<u8>`s, wait, and return them with the status.
            - **`.spawn()`**: start it and return a `Child` immediately, so your program keeps running. You then choose when to wait.

            The program is found through the `PATH` environment variable unless you give a path. If it doesn't exist, `spawn`, `output` and `status` return an `io::Error` with kind `NotFound`; that's an error in *starting* the program, not an exit code from it.

            ## Never build a shell command from data
            It's tempting to write `Command::new("sh").arg("-c").arg(format!("wc -c {path}"))`. Don't. The shell interprets its command text: spaces split arguments, and `;`, `|`, `` ` `` and `$( )` run commands. A file named `my notes.txt` becomes two arguments; a file named `$(rm -rf ~).txt` runs `rm`. This is **command injection**, one of the most common security vulnerabilities in software that shells out.

            `Command::new("wc").arg("-c").arg(path)` passes `path` as exactly one argument, directly to `wc`, with no shell in between. There is nothing to escape and nothing to inject. Use a shell only when you need shell features, and then pass data as separate positional arguments rather than formatting it into the script.

            ## Connecting streams with pipes
            By default a child inherits your streams. `Stdio` changes that:

            - `Stdio::inherit()`: share the parent's stream (the default for `status` and `spawn`).
            - `Stdio::piped()`: create a pipe so your program can write to the child's stdin or read its stdout and stderr.
            - `Stdio::null()`: connect to nothing (discard output, or give empty input).

            Feeding a child input and reading its output:

            ```rust
            use std::io::Write;
            use std::process::{Command, Stdio};

            let mut child = Command::new("sort")
                .env("LC_ALL", "C")
                .stdin(Stdio::piped())
                .stdout(Stdio::piped())
                .spawn()?;
            {
                let mut stdin = child.stdin.take().expect("stdin is piped");
                stdin.write_all(b"pear\napple\n")?;
            }   // dropping stdin closes the pipe: sort sees end of input
            let output = child.wait_with_output()?;
            ```

            Two details make or break this code:

            - **Close stdin when you're done writing.** `sort` can't produce output until it knows the input has ended, and it only knows that when the write end of the pipe closes. `take()` moves the handle out of the `Child` so it's dropped at the end of the block.
            - **Pipes are small** (often 64 KB on Linux). If the child writes a lot of output while you're still writing input, and nobody is reading its output, the child blocks on its full output pipe, stops reading its input, and you block on your full input pipe. Both wait forever: a **deadlock**. `sort` happens to be safe, because it reads everything before writing anything, but for a program that streams, write the input from a separate thread while the main thread reads the output.

            `.env("LC_ALL", "C")` sets a variable for the child only. Here it makes `sort` compare raw bytes instead of following the user's language settings, so the result is the same on every machine. `.env_clear()` starts the child with an empty environment, which is useful when the parent's variables shouldn't leak.

            ## Exit status
            `ExitStatus::success()` is true for exit code 0. `status.code()` returns `Option<i32>`: `Some(code)` if the program exited normally, and **`None` if it was killed by a signal** on Unix. On Unix, `std::os::unix::process::ExitStatusExt` adds `status.signal()` to find out which one. Shells report a signal death as `128 + N`, which is why Ctrl-C shows exit status 130.

            A `Child` that you never wait for doesn't disappear when it exits: it remains a **zombie**, an entry in the process table holding its exit status until the parent collects it. Dropping a `Child` in Rust does *not* wait. Always call `wait`, `wait_with_output`, or `output`.

            ## Signals
            A **signal** is a small asynchronous notification the kernel delivers to a process. The ones you'll meet:

            - `SIGINT` (2): Ctrl-C in a terminal. Default action: terminate.
            - `SIGTERM` (15): a polite request to stop, sent by `kill`, Docker, Kubernetes and systemd at shutdown. A service should finish in-flight work and exit.
            - `SIGKILL` (9): stop immediately. It can't be caught or ignored, so no cleanup runs; Kubernetes sends it after the grace period if `SIGTERM` was ignored.
            - `SIGPIPE`: writing to a pipe with no reader. Rust ignores it, so you get a `BrokenPipe` error instead (previous lesson).

            The standard library doesn't provide signal handlers, because doing almost anything inside one is unsafe. Use a crate: `ctrlc` for simple tools, `signal-hook`, or `tokio::signal` in async services. The usual pattern is that the handler just sets a flag or sends a message, and the main loop notices it and shuts down cleanly. `child.kill()` sends `SIGKILL` to a child (on Windows, it terminates the process).

            ## Common mistakes
            - Formatting untrusted text into `sh -c`.
            - Forgetting to drop a piped stdin, so the child waits forever for more input.
            - Treating a missing exit code as success, or as code 1.
            - Spawning children and never waiting for them.
            - Depending on the user's locale or environment in a child's output.

            ## Try it locally
            1. Create a file named `my notes.txt` and get its size with `wc -c`, first via `sh -c` and `format!`, then with `.arg(path)`.
            2. In a terminal, run `sleep 100`, press Ctrl-C, and `echo $?`. Then run it from Rust and print `status.code()` and `status.signal()`.
            3. Pipe a large input through `cat` by writing all of it before reading. Find the size at which it hangs, then fix it with a writer thread.
            """,
        Example = """
            use std::process::Command;

            fn file_size(path: &str) -> std::io::Result<String> {
                // `path` is one argument to wc: no shell, nothing to inject.
                let output = Command::new("wc").arg("-c").arg(path).output()?;
                if !output.status.success() {
                    return Err(std::io::Error::other(String::from_utf8_lossy(&output.stderr).into_owned()));
                }
                Ok(String::from_utf8_lossy(&output.stdout).split_whitespace().next().unwrap_or("0").to_string())
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Run programs safely and read how they ended",
            "Write three functions that start other programs with `Command`. The grader checks exit codes and signals, pipes 20,000 lines through `sort`, and passes file names containing spaces and `$( )`. Don't write a `main` function.",
            [
                "`fn run_capture(program: &str, args: &[&str]) -> io::Result<(i32, String)>` returns the exit code (or -1 if the program was killed by a signal) and its standard output; a program that can't be started returns the error",
                "`fn sort_lines(input: &str) -> io::Result<String>` pipes the input through `sort` with `LC_ALL=C` and returns its output",
                "`fn file_size(path: &str) -> io::Result<String>` runs `wc -c` on the path, passed as a single argument without a shell, and returns just the number; a failure from `wc` is an error",
            ],
            """
            use std::io::{self, Write};
            use std::process::{Command, Stdio};

            fn run_capture(program: &str, args: &[&str]) -> io::Result<(i32, String)> {
                todo!()
            }

            fn sort_lines(input: &str) -> io::Result<String> {
                todo!()
            }

            fn file_size(path: &str) -> io::Result<String> {
                todo!()
            }
            """,
            null,
            [],
            "`Command::new(program).args(args).output()?` gives the status and stdout; `status.code().unwrap_or(-1)` handles signals. For `sort_lines`, spawn with piped stdin and stdout, write the input inside a block that drops the handle from `child.stdin.take()`, then call `wait_with_output()`. For `file_size`, `.arg(\"-c\").arg(path)` and take the first word of the output; use `io::Error::other(...)` when the status isn't success.",
            ["Exit codes and signal deaths are reported", "Input is piped through sort without deadlock", "File names are never interpreted by a shell"]),
    };

    private static Lesson RustSerdeFormats(Lesson lesson) => lesson with
    {
        Subtitle = "Turn Rust types into JSON and back with serde, control the format with attributes, and reject input that doesn't match.",
        Concept = "Serialization converts in-memory values into bytes that can be stored or sent; deserialization converts them back. serde separates the two halves: deriving `Serialize` and `Deserialize` describes your types once, and a format crate such as `serde_json` or `toml` handles the bytes. Attributes adjust names, defaults and strictness without hand-written parsing.",
        Body = """
            ## The problem
            A struct in memory is a layout of bytes that only makes sense to this program, on this machine, compiled by this compiler. To save it to a file, send it over a network, or exchange it with a program written in another language, you need an agreed **format**: JSON, TOML, YAML, MessagePack, CSV, or a compact binary encoding. **Serialization** turns a value into that format; **deserialization** parses it back and checks it on the way.

            Writing that code by hand for every type is tedious and error-prone. Rust's ecosystem standardized on **serde**, which splits the job in two:

            - Your types derive **`Serialize`** and **`Deserialize`**. The derive macros generate code that describes each type's structure: "a struct with these fields, of these types".
            - A **format crate** turns that description into bytes and back: `serde_json`, `toml`, `csv`, `postcard`, and others for YAML and MessagePack.

            Any serde type works with any serde format. Switching from JSON to TOML is a one-line change.

            ## A first example
            In `Cargo.toml`: `serde = { version = "1", features = ["derive"] }` and `serde_json = "1"`.

            ```rust
            use serde::{Deserialize, Serialize};

            #[derive(Debug, Serialize, Deserialize)]
            struct ServiceConfig {
                name: String,
                port: u16,
                tags: Vec<String>,
            }

            let config: ServiceConfig = serde_json::from_str(r#"{"name": "api", "port": 9000, "tags": []}"#)?;
            let text = serde_json::to_string_pretty(&config)?;
            ```

            `r#"..."#` is a **raw string literal**: backslashes and quotes inside it are taken literally, which is convenient for JSON.

            Deserialization **validates as it parses**. The target type is the schema: `port: u16` means a JSON string, a negative number or `70000` is rejected with an error naming the problem and the line and column where it occurred. If parsing succeeds, you have a fully typed, valid `ServiceConfig`. There's no separate "check the JSON" step to forget.

            The type annotation `let config: ServiceConfig` is what tells `from_str` what to produce, the same way it tells `parse()` which number type to produce.

            ## Attributes
            serde's behaviour is adjusted with attributes on the type or on individual fields.

            **Names.** JSON APIs often use `camelCase` while Rust uses `snake_case`. `#[serde(rename_all = "camelCase")]` on the struct maps `log_level` to `logLevel` for every field; `#[serde(rename = "logLevel")]` does it for one.

            **Defaults.** A field missing from the input is an error unless you say what to use instead:

            - `#[serde(default)]` uses the type's `Default` value: 0, an empty string, an empty `Vec`.
            - `#[serde(default = "default_port")]` calls a function you write: `fn default_port() -> u16 { 8080 }`.
            - A field of type `Option<T>` is automatically `None` when missing.

            **Strictness.** By default serde **ignores fields it doesn't recognise**. That's convenient for reading other people's APIs that add fields over time, and dangerous for configuration you own: a user who types `"prot": 9000` gets the default port with no warning. `#[serde(deny_unknown_fields)]` turns that into an error that names the unexpected field.

            **Output.** `#[serde(skip_serializing_if = "Option::is_none")]` leaves out empty optional fields; `#[serde(skip)]` excludes a field entirely, such as a cache or a password.

            **Enums.** By default an enum variant is written as `{"VariantName": data}`. `#[serde(tag = "type")]` writes it as `{"type": "VariantName", ...fields}`, the shape most JSON APIs use for "one of several kinds of message".

            ## Choosing a format
            - **JSON**: universal, readable, the default for web APIs. Numbers have no fixed size, so very large integers need care across languages.
            - **TOML**: designed for configuration files people edit; Cargo uses it.
            - **YAML**: common in infrastructure (Kubernetes); flexible but full of surprises in its parsing rules.
            - **Binary formats** (`postcard`, `bincode`, MessagePack, Protocol Buffers): smaller and faster, not human-readable. For data stored on disk or exchanged between services, pick one with a stable, documented encoding.

            Whatever you choose, a serialized format is a **contract**. Once files exist on disk or another service depends on your messages, renaming a field is a breaking change. Add fields with defaults; remove fields only after every reader stops needing them.

            ## Untrusted input
            Deserializing data from the network is parsing untrusted input, and the security lessons apply:

            - Limit the size of the input *before* parsing; a 2 GB JSON body will be read into memory if you let it.
            - Prefer `deny_unknown_fields` for inputs you define, so mistakes are loud.
            - Remember that a successful parse proves the *shape* is right, not that the *values* make sense. A port of 0 or an empty name still needs checking. A common pattern is to deserialize into a plain struct, then convert it into a validated domain type with `TryFrom`.

            ## Dynamic data
            When you genuinely don't know the structure in advance, `serde_json::Value` holds any JSON: objects, arrays, numbers and so on, which you inspect at runtime. Use it at the edges, and convert to typed structs as soon as you know what you have.

            ## Common mistakes
            - Accepting unknown fields in your own configuration format, so typos are silently ignored.
            - Making every field an `Option` to avoid errors, then checking for `None` everywhere.
            - Renaming a Rust field and silently breaking every stored file and client.
            - Treating "it deserialized" as "it's valid".

            ## Try it locally
            1. Create a project with `serde` (with the `derive` feature) and `serde_json`, and parse the example config. Then misspell a field and read the error.
            2. Add `deny_unknown_fields` and try the misspelling again.
            3. Swap `serde_json` for `toml` (`toml::from_str`, `toml::to_string`) and convert the same struct to TOML.
            """,
        Example = """
            use serde::{Deserialize, Serialize};

            #[derive(Debug, Serialize, Deserialize)]
            #[serde(rename_all = "camelCase", deny_unknown_fields)]
            struct ServiceConfig {
                name: String,
                #[serde(default = "default_port")]
                port: u16,
                #[serde(default)]
                tags: Vec<String>,
            }

            fn default_port() -> u16 {
                8080
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Read and write a strict JSON config",
            "Define a configuration type with serde attributes and two functions that parse and print it. `serde` (with derive) and `serde_json` are available in the grader. Don't write a `main` function.",
            [
                "`#[derive(Debug, PartialEq, Serialize, Deserialize)] struct ServiceConfig` with `name: String` (required), `port: u16` (default 8080), `tags: Vec<String>` (default empty) and `log_level: String` (default `info`, written in JSON as `logLevel`)",
                "Unknown fields are rejected with serde's \"unknown field\" error, and a missing `name` with its \"missing field\" error",
                "`fn parse_config(json: &str) -> Result<ServiceConfig, String>` returns serde's error message on failure; `fn to_pretty(config: &ServiceConfig) -> String` returns indented JSON that `parse_config` reads back to the same value",
            ],
            """
            use serde::{Deserialize, Serialize};

            #[derive(Debug, PartialEq, Serialize, Deserialize)]
            struct ServiceConfig {
                // Add the fields and serde attributes here.
            }

            fn parse_config(json: &str) -> Result<ServiceConfig, String> {
                todo!()
            }

            fn to_pretty(config: &ServiceConfig) -> String {
                todo!()
            }
            """,
            null,
            [],
            "Put `#[serde(rename_all = \"camelCase\", deny_unknown_fields)]` on the struct. Use `#[serde(default = \"default_port\")]` with `fn default_port() -> u16 { 8080 }`, a similar function for the log level, and plain `#[serde(default)]` for `tags`. Then `serde_json::from_str(json).map_err(|e| e.to_string())` and `serde_json::to_string_pretty(config).unwrap()`.",
            ["Every field and default is read correctly", "Typos and missing fields are rejected", "Output round-trips through the parser"]),
    };

    private static Lesson RustCliProject(Lesson lesson) => lesson with
    {
        Subtitle = "Build `minigrep`, a small but honest Unix tool: options, files or standard input, correct output for pipes, errors on stderr, and grep's exit codes.",
        Concept = "A good command-line tool is a pure core wrapped in a thin shell. Parse arguments into a config, run the search against whatever readers and writers you're given, and report the outcome through output streams and an exit code. Keeping the real process (args, stdin, stdout, exit) in `main` makes the whole tool testable without starting a process.",
        Body = """
            ## What you're building
            This project pulls the module together into one tool, a simplified `grep`:

            ```text
            minigrep [-i] [-n] [-c] PATTERN [FILE...]
            ```

            - It prints every line containing `PATTERN`.
            - `-i` ignores case, `-n` prefixes each line with its line number, and `-c` prints only the number of matching lines.
            - With no files it reads standard input, so it works in pipelines: `cat log.txt | minigrep error`.
            - With more than one file, each output line starts with its file name: `a.txt:the line`.
            - Exit code 0 if anything matched, 1 if nothing did, and 2 if there was an error. An unreadable file is reported on stderr and the remaining files are still searched.

            Those rules aren't arbitrary: they're how `grep` behaves, which means scripts and people already know how to use your tool.

            ## Architecture: a testable core
            The design decision that matters most is where the real operating system is touched. Put all of it in `main`:

            ```rust
            use std::io::{self, BufWriter, Write};
            use std::process::ExitCode;

            fn main() -> ExitCode {
                let args: Vec<String> = std::env::args().skip(1).collect();
                let stdin = io::stdin();
                let mut input = stdin.lock();
                let mut output = BufWriter::new(io::stdout().lock());
                let mut errors = io::stderr();
                let code = run(&args, &mut input, &mut output, &mut errors);
                let _ = output.flush();
                ExitCode::from(code as u8)
            }
            ```

            Everything else lives in `run`, which takes the arguments and three streams as parameters:

            ```rust
            fn run(args: &[String], stdin: &mut dyn BufRead, stdout: &mut dyn Write, stderr: &mut dyn Write) -> i32
            ```

            `&mut dyn BufRead` is a **trait object** (from the traits lesson): "some type that implements `BufRead`, decided at runtime". In `main` it's the locked real stdin; in tests it's a `Cursor` over a string, and stdout is a `Vec<u8>` you can inspect afterwards. This lesson's grader calls `run` exactly that way. It never starts a process, yet it exercises every rule above.

            The `as u8` cast in `main` is safe because `run` only returns 0, 1 or 2; a comment saying so would be good practice.

            ## Suggested structure inside run
            1. **Parse** the arguments into an options struct, or report a usage error: the message and the usage line on stderr, return 2. Reuse your parser from the arguments lesson.
            2. **Choose the inputs**: no files means stdin; otherwise each file in turn.
            3. **Search** each input with one function, generic over the reader: `fn search<R: BufRead>(reader: R, label: Option<&str>, options: &Options, out: &mut dyn Write) -> io::Result<usize>`. It returns the number of matches so `run` can compute the exit code, and the `label` is the file name prefix, or `None`.
            4. **Handle errors per file.** `File::open(path)` fails → write `minigrep: <path>: <error>` to stderr, remember that an error happened, and continue with the next file.
            5. **Decide the exit code** at the end: 2 if any error happened, otherwise 0 or 1 by the match count.

            Case-insensitive matching is simplest done by lowercasing both the pattern and each line before `contains`. That's correct for most text, though full Unicode case folding has edge cases a production tool would handle with a dedicated crate.

            ## Milestones beyond the grader
            The graded exercise is the core. To turn it into a project you can show and explain, work through these on your own computer:

            1. **A real binary.** `cargo new minigrep`, write `main` as above, and try it on real files and in pipelines: `cargo run -- -n fn src/main.rs`, `ls | cargo run -- .rs`.
            2. **Integration tests that run the binary.** Put tests in `tests/cli.rs` that start your compiled program with `std::process::Command` (Cargo exposes its path to integration tests as `env!("CARGO_BIN_EXE_minigrep")`), feed it stdin, and check stdout, stderr and the exit status. These test what users actually experience.
            3. **Behave well in pipes.** Run `cargo run -- the big.txt | head -n 1` and make sure it exits quietly instead of panicking with a broken pipe.
            4. **Measure.** Generate a large file (a few hundred MB), time a release build against `grep` with `time`, and profile where the time goes. Try reading with a bigger buffer, avoiding the per-line `to_lowercase` allocation, or searching bytes instead of `String`s. Write down what you measured, what you changed, and what improved.
            5. **Extend.** Add `-r` to search directories recursively with `fs::read_dir`, or `-v` to invert the match. Each addition needs tests first.
            6. **Explain it.** Be ready to walk someone through why `run` takes trait objects, why errors go to stderr, why the tool continues after a bad file, and what your measurements showed.

            Write the implementation yourself. Use the compiler's messages, the standard library documentation (`cargo doc --open` or doc.rust-lang.org/std) and your earlier exercises. That's how the knowledge sticks.

            ## Common mistakes
            - Printing results with `println!` inside `run`, which bypasses the injected stdout and makes the output impossible to test.
            - Stopping at the first unreadable file, or exiting 0 after an error.
            - Prefixing file names when there's only one file (grep doesn't).
            - Writing errors to stdout, where they mix with results in a pipeline.
            """,
        Example = """
            fn run(args: &[String], stdin: &mut dyn BufRead, stdout: &mut dyn Write, stderr: &mut dyn Write) -> i32 {
                // parse -> choose inputs -> search each -> report errors -> exit code
                0
            }

            // In a test:
            let mut input = std::io::Cursor::new(b"one TODO\ntwo\n".to_vec());
            let (mut out, mut err) = (Vec::new(), Vec::new());
            let code = run(&["TODO".to_string()], &mut input, &mut out, &mut err);
            assert_eq!((code, out), (0, b"one TODO\n".to_vec()));
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Build minigrep's core",
            "Implement `run` for the tool described in the lesson. The grader calls it with in-memory streams and real files in a temporary folder, and checks stdout, stderr and the exit code for each case. You may add any helper functions and types. Don't write a `main` function.",
            [
                "`fn run(args: &[String], stdin: &mut dyn BufRead, stdout: &mut dyn Write, stderr: &mut dyn Write) -> i32` with options `-i`, `-n` and `-c` before or after the pattern; reads stdin when no files are given",
                "Output: matching lines; with `-n` prefix `<number>:`; with several files prefix `<file>:` (before the line number); with `-c` print only `<count>` per input (`<file>:<count>` with several files)",
                "Errors: a missing pattern or unknown option prints a message and `usage: minigrep [-i] [-n] [-c] PATTERN [FILE...]` to stderr and returns 2; an unreadable file prints `minigrep: <file>: <error>` to stderr, the other files are still searched, and the result is 2",
                "Exit code otherwise: 0 when at least one line matched, 1 when none did",
            ],
            """
            use std::fs::File;
            use std::io::{self, BufRead, BufReader, Write};

            fn run(args: &[String], stdin: &mut dyn BufRead, stdout: &mut dyn Write, stderr: &mut dyn Write) -> i32 {
                todo!()
            }
            """,
            null,
            [],
            "Start with parsing (reuse your arguments-lesson code) and a `search<R: BufRead>(reader: R, label: Option<&str>, ...) -> io::Result<usize>` helper that writes to `stdout` and returns the match count. Call it with `stdin` when there are no files, and with `BufReader::new(file)` for each file, passing `Some(path)` as the label only when there are two or more files. Keep an `had_error` flag and decide the exit code at the very end.",
            ["Standard input and files are searched", "Options shape the output exactly like grep", "Errors go to stderr and don't stop other files", "Exit codes distinguish matches, no matches and errors"]),
    };
}
