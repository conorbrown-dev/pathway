namespace Pathway.Content;

static partial class Curriculum
{
    // Authored lessons for the "Low-level foundations" module: bits and byte order, slices as
    // zero-copy buffers, a checked binary parser, and measuring performance. Fixtures live in
    // sandbox-broker/src/evaluator.mjs.

    private static Lesson RustBytesEndianness(Lesson lesson) => lesson with
    {
        Title = "Bits, masks, and byte order",
        Subtitle = "Read and write the individual bits and bytes that file formats, network protocols and hardware registers are made of.",
        Concept = "Every integer is a fixed row of bits. Bitwise operators combine, test and move those bits, which is how flags, packed fields and hardware registers work. When a number larger than one byte is stored or sent, its bytes must be put in some order; little-endian puts the least significant byte first, big-endian the most significant, and every format must say which it uses.",
        Body = """
            ## Bits and how to write them
            A `u8` is 8 bits; each bit is worth a power of two. Numbering bits from the right starting at 0, bit 0 is worth 1, bit 1 is worth 2, bit 2 is worth 4, and so on up to bit 7, worth 128. The value is the sum of the bits that are on: `0b0000_0101` has bits 0 and 2 on, so it's 1 + 4 = 5.

            Rust lets you write numbers in whichever base makes the meaning clearest:

            - **Binary**, `0b0000_0101`: when individual bits matter.
            - **Hexadecimal**, `0x2F`: each hex digit is exactly 4 bits, so two hex digits are one byte. Byte values, colours, addresses and protocol constants are usually written in hex. `0xFF` is a byte with all bits on, 255.
            - **Octal**, `0o755`: three bits per digit, still used for Unix file permissions, where each digit is read/write/execute for one group of users.

            Print them the same way: `{:b}`, `{:x}`, `{:o}`, with padding like `{:08b}` to show all eight bits of a byte.

            ## Bitwise operators
            Each operator works on every bit position independently:

            - `a & b` (AND): a bit is on only if it's on in **both**. Used to *test* or *keep* certain bits.
            - `a | b` (OR): on if it's on in **either**. Used to *turn bits on*.
            - `a ^ b` (XOR): on if it's on in **exactly one**. Used to *flip* bits.
            - `!a` (NOT): every bit inverted. (C uses `~`; Rust uses `!` for both booleans and integers.)
            - `a << n`: move every bit `n` places to the left, filling with zeros; each step doubles the value.
            - `a >> n`: move bits to the right; each step halves it. For unsigned types the gap fills with zeros; for signed types it copies the sign bit, so negative numbers stay negative.

            A **mask** is a value with exactly the bits you care about turned on. `1u8 << 3` is `0b0000_1000`, a mask for bit 3. With it:

            ```rust
            let flags: u8 = 0b0000_0101;
            let mask = 1u8 << 3;

            let is_on = flags & mask != 0;    // test: is bit 3 on?
            let set = flags | mask;           // turn bit 3 on
            let cleared = flags & !mask;      // turn bit 3 off: AND with everything except bit 3
            let toggled = flags ^ mask;       // flip bit 3
            ```

            Note that turning a bit off uses `& !mask`, not `^`: XOR *flips*, so applying it to a bit that was already off turns it on. That mix-up is a classic bug.

            Shifting by the type's width or more (`1u8 << 8`) has no meaningful result. In a debug build Rust panics with "attempt to shift left with overflow"; in release it masks the shift amount, giving a surprising value. When the shift amount comes from data, use `checked_shl`, which returns `None` instead.

            ## Flags and packed fields
            Many formats squeeze several small values into one integer:

            - **Flag sets**: each bit means one yes/no option. TCP's header packs SYN, ACK, FIN and others into bits; Unix permissions are bits too. The `bitflags` crate generates a typed API for this.
            - **Packed fields**: several numbers of odd widths in one integer. A common example from embedded displays is **RGB565**: a colour stored in 16 bits as 5 bits of red, 6 of green, 5 of blue:

            ```text
            bit:  15 14 13 12 11 | 10 9 8 7 6 5 | 4 3 2 1 0
                  r  r  r  r  r  | g  g g g g g | b b b b b
            ```

            To pack an 8-bit red into 5 bits, keep its top 5 bits (`red >> 3`), then move them into position (`<< 11`). Green keeps its top 6 bits (`>> 2`) and moves to bit 5; blue keeps its top 5 and stays at the bottom. Combine with `|`. Unpacking reverses it: shift right to bring a field down to bit 0, mask off the other fields with `& 0x1F` (five ones) or `& 0x3F` (six ones), then shift back up to 8-bit range. The bits dropped during packing are gone, so a round trip returns each channel with its low bits cleared.

            Convert to the wider type *before* shifting: `(red as u16) << 11`, or `u16::from(red) << 11`. Shifting a `u8` left by 11 overflows before it ever becomes a `u16`.

            ## Byte order
            A `u32` takes four bytes in memory, and those bytes must go in some order. Take `0x12345678`; its most significant byte is `0x12` and its least significant is `0x78`.

            - **Little-endian** stores the *least* significant byte first: `78 56 34 12`. x86 and almost all ARM processors work this way, so most computers you'll use are little-endian.
            - **Big-endian** stores the *most* significant byte first: `12 34 56 78`. This is the conventional order for network protocols ("network byte order") and many file formats.

            Inside one program, byte order never matters: the processor handles it. It matters the moment bytes leave the program, into a file, onto the network, or across to hardware, because the reader must reassemble them in the same order. Get it wrong and `1` becomes `16777216`.

            Rust makes the order explicit in every conversion:

            ```rust
            let bytes = 0x12345678u32.to_be_bytes();   // [0x12, 0x34, 0x56, 0x78]
            let bytes = 0x12345678u32.to_le_bytes();   // [0x78, 0x56, 0x34, 0x12]
            let n = u32::from_le_bytes([0x78, 0x56, 0x34, 0x12]);   // 0x12345678
            ```

            There's also `to_ne_bytes` and `from_ne_bytes`, for the **native** order of the machine running the code. Avoid them for anything stored or sent: the result differs between machines.

            `from_le_bytes` takes exactly four bytes, an array `[u8; 4]`. To get one from a longer slice, take the first four with `bytes.get(..4)` (which returns `None` if there aren't four) and convert with `try_into()`.

            ## Common mistakes
            - Clearing a bit with `^` instead of `& !mask`.
            - Shifting before widening the type.
            - Assuming the machine's byte order, or using `to_ne_bytes` for data that leaves the program.
            - Indexing `bytes[3]` without checking the length, turning short input into a panic.

            ## Try it locally
            1. Print `0x12345678u32.to_le_bytes()` and `to_be_bytes()` with `{:02x?}`.
            2. Print a number with `{:08b}` before and after each of `|`, `& !` and `^` with the same mask.
            3. Run `xxd` or `hexdump -C` on a small binary file, such as a PNG image, and find its 8-byte signature and the big-endian width and height in its header.
            """,
        Example = """
            let mask = 1u8 << 3;
            let flags = 0b0000_0101u8 | mask;      // turn bit 3 on
            let cleared = flags & !mask;            // and off again
            let packed = (u16::from(255u8) >> 3) << 11; // red in RGB565
            let n = u32::from_le_bytes([0x78, 0x56, 0x34, 0x12]);
            assert_eq!(n, 0x1234_5678);
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Work with bits, fields, and byte order",
            "Write four functions that manipulate bits and bytes directly. None may panic on any input. Don't write a `main` function.",
            [
                "`fn read_u32_le(bytes: &[u8]) -> Option<u32>` reads the first four bytes as a little-endian `u32`, or `None` if there are fewer than four",
                "`fn set_bit(flags: u8, bit: u32, on: bool) -> u8` turns bit number `bit` on or off and leaves the others alone; a bit number of 8 or more returns `flags` unchanged",
                "`fn pack_rgb565(red: u8, green: u8, blue: u8) -> u16` keeps the top 5, 6 and 5 bits of each channel and packs them red-high, green-middle, blue-low",
                "`fn unpack_rgb565(color: u16) -> (u8, u8, u8)` reverses it, returning each channel shifted back to 8-bit range (low bits zero)",
            ],
            """
            fn read_u32_le(bytes: &[u8]) -> Option<u32> {
                todo!()
            }

            fn set_bit(flags: u8, bit: u32, on: bool) -> u8 {
                todo!()
            }

            fn pack_rgb565(red: u8, green: u8, blue: u8) -> u16 {
                todo!()
            }

            fn unpack_rgb565(color: u16) -> (u8, u8, u8) {
                todo!()
            }
            """,
            null,
            [],
            "`let first: [u8; 4] = bytes.get(..4)?.try_into().ok()?;` then `u32::from_le_bytes(first)`. For `set_bit`, return early when `bit >= 8`, build `1u8 << bit`, and use `|` to set or `& !mask` to clear. Pack with `((red as u16 >> 3) << 11) | ((green as u16 >> 2) << 5) | (blue as u16 >> 3)`; unpack with shifts and the masks `0x1f` and `0x3f`.",
            ["Bytes are read little-endian, safely", "Bits are set and cleared without disturbing others", "Colors pack into 16 bits", "Colors unpack to 8-bit channels"]),
    };

    private static Lesson RustSlicesBuffers(Lesson lesson) => lesson with
    {
        Title = "Slices, bounds, and zero-copy buffers",
        Subtitle = "Treat byte slices as views into a buffer: split them, search them and walk them in chunks without copying, and do the offset arithmetic so it can never overflow.",
        Concept = "A slice (`&[u8]`) is a pointer and a length: a borrowed view into memory someone else owns. Systems code reads data into a buffer once and then hands out slices of it instead of copying. Every offset that comes from data must be checked, including the addition that computes where a range ends, because an attacker-controlled length can make that addition overflow.",
        Body = """
            ## Arrays, Vecs and slices, one more time
            - `[u8; 16]` is an **array**: exactly 16 bytes, size fixed at compile time, stored inline (on the stack for a local).
            - `Vec<u8>` is an owned, growable buffer on the heap.
            - `&[u8]` is a **slice**: a borrowed view of some contiguous bytes, which may be part of an array, part of a `Vec`, or part of another slice. It's two words: the address of the first byte and the length.

            A function that reads bytes should almost always take `&[u8]`; it then works on all three without copying. A function that fills a buffer takes `&mut [u8]`, which is exactly what `Read::read` does.

            ## Bounds: panic or check
            Indexing with `buf[i]` or `&buf[start..end]` is **bounds-checked**: outside the slice, the program panics instead of reading memory it shouldn't. That check is what makes out-of-bounds reads, the source of countless C vulnerabilities, impossible in safe Rust. But a panic in a server parsing network input is still a crash. When the position comes from data, use the methods that return `Option`:

            - `buf.get(i)` and `buf.get(start..end)`.
            - `buf.split_at_checked(mid)` returns `Some((before, after))`, or `None` if `mid` is past the end.
            - `buf.first()`, `buf.last()`, `buf.split_first()`.

            ## The overflow in the bounds check
            A common hand-written check looks correct and isn't:

            ```rust
            if offset + len > buffer.len() {
                return None;
            }
            let field = &buffer[offset..offset + len];
            ```

            If `offset` and `len` come from a file or a packet, an attacker can choose them. With `len = usize::MAX`, `offset + len` overflows. In a debug build that panics; in a release build it **wraps around** to a small number, the check passes, and in C the following read would run off the buffer. Real vulnerabilities in image libraries, font parsers and network stacks have exactly this shape.

            Do the arithmetic with `checked_add` and let `get` do the comparison:

            ```rust
            fn read_at(buffer: &[u8], offset: usize, len: usize) -> Option<&[u8]> {
                let end = offset.checked_add(len)?;
                buffer.get(offset..end)
            }
            ```

            The same applies to subtraction (`checked_sub`) and multiplication (`checked_mul`, as in "count times record size").

            ## Zero-copy: views instead of copies
            Suppose a packet has a 2-byte header and a body. Copying them into new `Vec`s costs an allocation and a copy each. Slicing costs nothing:

            ```rust
            let (header, body) = packet.split_at_checked(2)?;
            ```

            `header` and `body` are views into `packet`: same memory, new start and length. The borrow checker guarantees `packet` outlives them and isn't modified while they exist. This is **zero-copy** parsing, and it's a large part of why Rust parsers can be both safe and fast. Libraries take it further: the `bytes` crate's `Bytes` type is a reference-counted buffer whose slices can be sent between threads and tasks, used throughout Tokio and Hyper.

            You can check that a slice really points into the original with `slice.as_ptr()`: for a zero-copy split, the header's pointer equals the packet's.

            ## Walking a buffer
            Slices come with iterators that never index out of bounds:

            - `chunks(n)`: consecutive pieces of `n` bytes; the last may be shorter. `chunks_exact(n)` gives only full pieces and exposes the leftover with `.remainder()`. Records of fixed size are `chunks_exact`.
            - `windows(n)`: every overlapping run of `n` bytes: `[a, b, c, d]` with `windows(2)` gives `[a, b]`, `[b, c]`, `[c, d]`. Searching for a byte pattern is `haystack.windows(needle.len()).position(|w| w == needle)`. (For large inputs a specialised algorithm such as the `memchr` crate's is much faster.)
            - `split(|&b| b == b'\n')`: pieces between separator bytes.

            `chunks(0)` and `windows(0)` panic, since a size of zero makes no sense; guard against a zero that comes from data.

            ## Reusing buffers
            Allocating a new `Vec` for every message adds up in a hot loop. Reuse one:

            - `buf.clear()` sets the length to 0 but keeps the capacity, so the next fill doesn't allocate.
            - `buf.extend_from_slice(&data)` appends bytes.
            - `dest.copy_from_slice(&src)` copies between equal-length slices, the safe version of C's `memcpy`; it panics if the lengths differ, which is the right outcome for a logic error.
            - `vec![0u8; n]` makes a zeroed buffer to `read` into; `read` returns how many bytes it actually filled, and only `&buf[..n]` holds data.

            ## Common mistakes
            - `offset + len` in a bounds check on untrusted values.
            - Treating the whole buffer as data after a `read` that filled only part of it.
            - Copying into new `Vec`s where a slice would do.
            - `chunks(size)` with a `size` read from input that might be zero.

            ## Try it locally
            1. Write the unsafe-looking `offset + len` check and call it with `usize::MAX` in debug and release builds. Compare.
            2. Split a buffer and print `as_ptr()` of the original and both halves.
            3. Parse a buffer of fixed-size records with `chunks_exact` and handle `remainder()`.
            """,
        Example = """
            fn read_at(buffer: &[u8], offset: usize, len: usize) -> Option<&[u8]> {
                let end = offset.checked_add(len)?; // no wrap-around, ever
                buffer.get(offset..end)            // no panic past the end
            }

            let packet = [0x01, 0x02, b'h', b'i'];
            let (header, body) = packet.split_at_checked(2).unwrap(); // views, not copies
            assert_eq!(body.as_ptr(), packet[2..].as_ptr());
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Slice a buffer safely, without copying",
            "Write four functions over byte slices. Returned slices must point into the input, and no input, including `usize::MAX` offsets, may cause a panic. Don't write a `main` function.",
            [
                "`fn split_header(packet: &[u8], header_len: usize) -> Option<(&[u8], &[u8])>` splits without copying, or `None` if the packet is too short",
                "`fn read_at(buffer: &[u8], offset: usize, len: usize) -> Option<&[u8]>` returns `len` bytes starting at `offset`, or `None` if they don't all exist, including when `offset + len` would overflow",
                "`fn find_subslice(haystack: &[u8], needle: &[u8]) -> Option<usize>` returns the position of the first match (an empty needle matches at 0)",
                "`fn chunk_sums(data: &[u8], size: usize) -> Vec<u32>` sums each chunk of `size` bytes (the last may be shorter); a size of 0 returns an empty `Vec`",
            ],
            """
            fn split_header(packet: &[u8], header_len: usize) -> Option<(&[u8], &[u8])> {
                todo!()
            }

            fn read_at(buffer: &[u8], offset: usize, len: usize) -> Option<&[u8]> {
                todo!()
            }

            fn find_subslice(haystack: &[u8], needle: &[u8]) -> Option<usize> {
                todo!()
            }

            fn chunk_sums(data: &[u8], size: usize) -> Vec<u32> {
                todo!()
            }
            """,
            null,
            [],
            "`packet.split_at_checked(header_len)` does the first one. For `read_at`, compute `offset.checked_add(len)?` and use `buffer.get(offset..end)`. Search with `windows(needle.len()).position(...)` after handling the empty needle. Sum chunks with `data.chunks(size)` after returning early for size 0, converting each byte with `u32::from`.",
            ["Splits borrow instead of copy", "Offsets are checked without overflow", "Searching finds the first match", "Chunks are summed without panics"]),
    };

    private static Lesson RustBinaryParser(Lesson lesson) => lesson with
    {
        Subtitle = "Parse a binary file format from a specification into typed records, returning a precise error for every malformed input and never panicking, however hostile the bytes.",
        Concept = "A binary parser turns untrusted bytes into trusted types. Read through the input with a cursor that only ever takes bytes that exist, convert numbers with an explicit byte order, validate as you go, and report each kind of problem as its own error variant. A parser that can panic, allocate without limit, or accept trailing garbage is a vulnerability, which is why parsers are tested with truncated, corrupted and random input.",
        Body = """
            ## The format
            This lesson's parser reads a small format invented for practice, specified the way real formats are:

            ```text
            offset  size      field
            0       4         magic: the ASCII bytes "PWAY"
            4       1         version: must be 1
            5       2         record count, unsigned, big-endian
            7       ...       records, one after another:
                    4           id, unsigned, big-endian
                    1           name length in bytes (0-255)
                    n           name, UTF-8
            ```

            Every real binary format has this anatomy: PNG, ZIP, the ELF executables Linux runs, TLS records. A **magic number** at the start identifies the format, so a program can refuse a file that's not the right kind instead of misinterpreting it. A **version** lets the format evolve. Counts and lengths tell the reader how much follows, which is the framing lesson's length prefix again.

            ## Errors as a closed set
            The errors lesson showed how an enum makes every failure explicit. A parser is where that pays off most:

            ```rust
            #[derive(Debug, PartialEq)]
            enum ParseError {
                BadMagic,
                UnsupportedVersion(u8),
                Truncated,
                InvalidUtf8,
                TrailingBytes,
            }
            ```

            Each variant answers a different question for the caller: "is this even our format?", "is it a newer version we should tell the user to upgrade for?", "was the download cut off?", "is the file corrupt?". A single `String` error, or worse a panic, throws that information away.

            ## A cursor that can't overrun
            Instead of tracking an offset by hand and indexing, keep the *unread rest* of the input as a slice and take bytes off its front:

            ```rust
            struct Cursor<'a> {
                rest: &'a [u8],
            }

            impl<'a> Cursor<'a> {
                fn take(&mut self, count: usize) -> Result<&'a [u8], ParseError> {
                    let (taken, rest) = self.rest.split_at_checked(count).ok_or(ParseError::Truncated)?;
                    self.rest = rest;
                    Ok(taken)
                }

                fn u16_be(&mut self) -> Result<u16, ParseError> {
                    Ok(u16::from_be_bytes(self.take(2)?.try_into().unwrap()))
                }
            }
            ```

            `take` is the only place bytes are consumed, and it can't go past the end, so the whole parser inherits that guarantee. The `unwrap` in `u16_be` is a justified one: `take(2)` returned exactly two bytes, so the conversion to `[u8; 2]` can't fail. That's the kind of `unwrap` a reviewer accepts, ideally with a comment saying why.

            The `'a` lifetime (from the lifetimes lesson) ties every returned slice to the original input, not to the cursor, so a parsed name can borrow from the file's bytes with no copy until you decide to make a `String`.

            ## Parsing, step by step
            1. **Magic.** Take 4 bytes; if there aren't 4, or they aren't `PWAY`, it's `BadMagic`. Check this first, so a completely wrong file gets the most helpful error.
            2. **Version.** One byte; anything but 1 is `UnsupportedVersion(v)`, carrying the value for the error message.
            3. **Count.** Two bytes, big-endian.
            4. **Records.** For each: the id, the length byte, then exactly that many name bytes, validated as UTF-8 with `std::str::from_utf8`. An invalid sequence is `InvalidUtf8`; don't use `from_utf8_lossy` here, which would quietly accept a corrupt file.
            5. **Trailing bytes.** After the last record, the input must be empty. Extra bytes mean the count was wrong or something was appended. Accepting them silently has caused real security bugs, where two programs parsing the same file disagreed about what it contained.

            Every step that runs out of input produces `Truncated`.

            ## Untrusted counts
            The record count is attacker-controlled. Writing `Vec::with_capacity(count)` trusts it: here the worst case is only 65,535 records, but in a format with a 4-byte count, one small file could request gigabytes. Either push records one at a time (the `Vec` grows only as real records are parsed) or cap the reservation: `Vec::with_capacity(count.min(remaining_bytes / smallest_record_size))`.

            ## Testing a parser like an attacker
            Normal example-based tests show the parser accepts good input. The bugs live in bad input, so parsers get three more kinds of tests:

            - **Every truncation.** For a valid file of N bytes, parse each prefix `&valid[..end]` for every `end` from 0 to N-1. Every one must return an error, and never panic. It's a short loop that covers every "ran out of input here" path at once.
            - **Random input.** Generate thousands of random byte strings, including ones that start with a valid header, and require that `parse` returns rather than panicking. `std::panic::catch_unwind` turns a panic into a value a test can check. This lesson's grader does exactly this.
            - **Fuzzing**, the industrial version: a **fuzzer** like `cargo fuzz` (built on LLVM's libFuzzer) generates inputs, watches which code paths each one reaches, and mutates the interesting ones to reach more. It finds panics, overflows and hangs that humans don't think to test. Run it locally: `cargo install cargo-fuzz`, `cargo fuzz init`, write a fuzz target that calls `parse(data)`, and `cargo fuzz run`. Google's OSS-Fuzz has found tens of thousands of bugs this way in widely used parsers.

            ## Common mistakes
            - Indexing `bytes[offset]` on untrusted input.
            - Lossy UTF-8 conversion where invalid text means corruption.
            - Ignoring trailing bytes.
            - Pre-allocating from a count in the input.
            - Reporting every failure as one generic error.

            ## Try it locally
            Turn this into the project in the course design notes, a **binary file inspector**: a command-line tool that reads a file in this format, prints a readable summary, and exits with distinct codes for each error. Add a writer that produces files, a fuzz target that runs overnight, and a benchmark comparing reading the whole file at once with streaming it through a `BufReader`.
            """,
        Example = """
            fn parse(bytes: &[u8]) -> Result<Vec<Record>, ParseError> {
                let mut cursor = Cursor { rest: bytes };
                if cursor.take(4).map_err(|_| ParseError::BadMagic)? != b"PWAY" {
                    return Err(ParseError::BadMagic);
                }
                let version = cursor.u8()?;
                // ... version check, count, records ...
                if !cursor.rest.is_empty() {
                    return Err(ParseError::TrailingBytes);
                }
                Ok(records)
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Parse a binary format that fights back",
            "Implement `parse` for the format in the lesson. The grader checks valid files, each error variant, every truncation of a valid file, and 3,000 random inputs; any panic fails. Don't write a `main` function.",
            [
                "`#[derive(Debug, PartialEq)] struct Record { id: u32, name: String }` and `#[derive(Debug, PartialEq)] enum ParseError { BadMagic, UnsupportedVersion(u8), Truncated, InvalidUtf8, TrailingBytes }`",
                "`fn parse(bytes: &[u8]) -> Result<Vec<Record>, ParseError>`: magic `PWAY`, version 1, a big-endian `u16` count, then records of a big-endian `u32` id, a one-byte name length and a UTF-8 name",
                "Fewer than 4 bytes or wrong magic is `BadMagic`; running out of bytes anywhere after that is `Truncated`; bytes left after the last record are `TrailingBytes`",
                "No input of any kind makes `parse` panic",
            ],
            """
            #[derive(Debug, PartialEq)]
            struct Record {
                id: u32,
                name: String,
            }

            #[derive(Debug, PartialEq)]
            enum ParseError {
                BadMagic,
                UnsupportedVersion(u8),
                Truncated,
                InvalidUtf8,
                TrailingBytes,
            }

            fn parse(bytes: &[u8]) -> Result<Vec<Record>, ParseError> {
                todo!()
            }
            """,
            null,
            [],
            "Build the `Cursor` from the lesson with `take`, `u8`, `u16_be` and `u32_be`, all returning `Truncated` when input runs out. Map the first `take(4)` error to `BadMagic`. Use `std::str::from_utf8` for names, push records one by one, and check `cursor.rest.is_empty()` at the end.",
            ["Valid files parse with big-endian numbers", "Each problem has its own error", "Every truncation is reported, not a panic", "Random input never panics"]),
    };

    private static Lesson RustPerfCacheBenchmark(Lesson lesson) => lesson with
    {
        Title = "Measure performance: allocation, caches, and benchmarks",
        Subtitle = "Find out where time and memory actually go before changing anything: benchmark release builds correctly, understand why memory layout matters, and count allocations.",
        Concept = "Performance work starts with measurement, not guesses. Benchmark only optimized builds and stop the optimizer from deleting the work you're timing. Most real slowdowns come from waiting on memory, so contiguous data that fits in the processor's caches, and code that allocates less, often beat clever algorithms. A counting allocator makes allocation visible enough to test.",
        Body = """
            ## Measure first
            The most important performance habit is refusing to optimize without evidence. Programs spend their time in surprising places, and a change that "should" be faster often isn't. The loop is always the same:

            1. Write a benchmark or reproduce the slow case, and record the number.
            2. **Profile** to see where the time goes.
            3. Change one thing.
            4. Measure again, in the same conditions, and keep the change only if the number improved.

            ## Benchmarking correctly
            - **Release builds only.** `cargo run --release`, `cargo bench`. A debug build has optimizations off and extra checks on; timing it measures a different program, and the ranking of two approaches can even reverse.
            - **Keep the work alive.** The optimizer deletes computations whose results are never used, so a benchmark of `let _ = expensive();` may measure nothing. Pass inputs and results through `std::hint::black_box(value)`, which tells the compiler to assume the value is used in ways it can't see.
            - **Repeat and look at the spread.** Timings vary with CPU frequency, other programs and caches. One run is an anecdote. The **criterion** crate runs a function many times, reports a confidence interval, and tells you whether a change is statistically significant. Rust's built-in `#[bench]` is still unstable, so criterion (or `divan`) is the normal choice.
            - **Measure what users experience.** A 10% faster function that's 1% of the runtime saves 0.1%.

            For a quick check, `std::time::Instant::now()` before and `.elapsed()` after is fine, as long as the build is optimized and the work is kept alive.

            ## Profiling
            A **profiler** samples the program thousands of times a second, recording which function is running, and shows where the time accumulates.

            - **Linux**: `perf record --call-graph dwarf ./target/release/app`, then `perf report`. **cargo-flamegraph** turns that into a **flame graph**: an SVG where each bar is a function and its width is the share of time spent in it, which makes the hot path obvious at a glance.
            - **macOS**: Instruments' Time Profiler, or **samply** (cross-platform, opens in the Firefox Profiler).
            - **Windows**: Visual Studio's profiler or Windows Performance Analyzer.

            Add `debug = true` (or `"line-tables-only"`) under `[profile.release]` so profiles show function names and lines.

            ## Why memory layout matters
            A modern processor can do an addition in a fraction of a nanosecond, but fetching data from main memory takes on the order of 100 nanoseconds. To hide that gap, processors keep recently used memory in small, fast **caches**, typically three levels, each larger and slower than the last, with L1 answering in about a nanosecond. Memory moves into the cache in **cache lines** of 64 bytes, and the processor **prefetches** the next lines when it sees you reading sequentially.

            The consequence: **reading memory in order is fast, jumping around is slow.**

            - A `Vec<u32>` is contiguous; summing it streams through memory and the prefetcher keeps up.
            - A linked list, or a `Vec<Box<T>>`, puts each element wherever the allocator happened to find room. Each step can miss the cache and wait for main memory. The same O(n) loop can be many times slower.
            - In a 2D grid stored row by row, a loop that walks across rows reads sequentially; one that walks down columns jumps a whole row each step.

            A related technique is choosing between an **array of structs**, `Vec<Particle>` where each holds position, velocity, colour and more, and a **struct of arrays**, one `Vec` per field. A loop that only updates positions touches only the position array in the second layout, so every cache line it loads is useful. Game engines and data-processing libraries use this heavily.

            Big-O notation still matters for large inputs; a quadratic algorithm loses to a linear one eventually. But between algorithms of the same complexity, the constant factors, which come mostly from memory access and allocation, decide the winner.

            ## Allocation
            Every heap allocation is a call into the allocator, which must find a free block and record it, and later free it. That's typically tens of nanoseconds; in a tight loop running millions of times, allocations easily dominate. Common sources:

            - Building a `String` or `Vec` piece by piece without reserving: each time it fills, it reallocates and copies.
            - `format!` in a loop, which creates a new `String` every call.
            - Collecting into a temporary `Vec` just to iterate over it again.
            - `clone()` where a borrow would do.

            The fixes are usually small: `String::with_capacity(total)` when you can compute the size; chaining iterators instead of collecting in between; reusing one buffer with `clear()`; and doing work **in place**, like `Vec::dedup`, `retain` and `sort_unstable`, which allocate nothing.

            ## Making allocation visible
            Rust lets a program replace its **global allocator**, the code behind every `Box`, `Vec` and `String`. That makes it possible to count allocations exactly:

            ```rust
            use std::alloc::{GlobalAlloc, Layout, System};
            use std::sync::atomic::{AtomicUsize, Ordering};

            static ALLOCATIONS: AtomicUsize = AtomicUsize::new(0);
            struct Counting;

            unsafe impl GlobalAlloc for Counting {
                unsafe fn alloc(&self, layout: Layout) -> *mut u8 {
                    ALLOCATIONS.fetch_add(1, Ordering::Relaxed);
                    unsafe { System.alloc(layout) }
                }
                unsafe fn dealloc(&self, ptr: *mut u8, layout: Layout) {
                    unsafe { System.dealloc(ptr, layout) }
                }
            }

            #[global_allocator]
            static GLOBAL: Counting = Counting;
            ```

            Read the counter before and after a call and you have a deterministic test: "this function allocates exactly once." Unlike a timing, that number doesn't vary between runs or machines, so it can live in CI and catch regressions. (`unsafe` appears because the allocator contract has rules the compiler can't check; the unsafe lesson explains what that means.) For whole programs, the **dhat** crate and the `heaptrack` tool report every allocation site.

            ## Common mistakes
            - Benchmarking debug builds, or letting the optimizer delete the benchmark.
            - Optimizing code the profiler never showed as hot.
            - `Vec<Box<T>>` or linked structures where a plain `Vec<T>` would do.
            - Building strings with repeated `format!` or `+` in loops.

            ## Try it locally
            1. Sum a 4096 × 4096 grid of `u32` row by row and column by column in a release build, and time both.
            2. Profile a program of your own with `cargo flamegraph` or samply and find its widest bar.
            3. Add the counting allocator to an earlier exercise and count allocations in a loop; then cut them with `with_capacity` and buffer reuse.
            """,
        Example = """
            fn join_words(words: &[&str], separator: &str) -> String {
                let total: usize = words.iter().map(|w| w.len()).sum::<usize>()
                    + separator.len() * words.len().saturating_sub(1);
                let mut joined = String::with_capacity(total); // the only allocation
                for (index, word) in words.iter().enumerate() {
                    if index > 0 {
                        joined.push_str(separator);
                    }
                    joined.push_str(word);
                }
                joined
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Do the same work with fewer allocations",
            "Write three functions whose results are easy, but whose allocation counts are graded. The grader installs a counting global allocator and measures each call. Don't write a `main` function.",
            [
                "`fn join_words(words: &[&str], separator: &str) -> String` joins the words with the separator using exactly one allocation (zero for an empty list)",
                "`fn parse_numbers(line: &str) -> Vec<u32>` returns the comma-separated values that parse as `u32` (trimmed; others skipped), in order, with at most one allocation",
                "`fn dedup_sorted(values: &mut Vec<u32>)` removes repeated values from a sorted vector in place, without allocating",
            ],
            """
            fn join_words(words: &[&str], separator: &str) -> String {
                todo!()
            }

            fn parse_numbers(line: &str) -> Vec<u32> {
                todo!()
            }

            fn dedup_sorted(values: &mut Vec<u32>) {
                todo!()
            }
            """,
            null,
            [],
            "Compute the final length first and use `String::with_capacity` (the standard `join` method also allocates once). For numbers, count the valid parts in a first pass, `Vec::with_capacity(count)`, then fill it in a second pass with no intermediate `Vec`. `Vec::dedup` works in place.",
            ["Joining allocates once", "Parsing allocates at most once", "Deduplication allocates nothing"]),
    };
}
