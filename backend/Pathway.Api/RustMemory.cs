static partial class Curriculum
{
    // Authored lessons for the start of the "Memory-safe Rust" module: where values live, then
    // ownership, borrowing and lifetimes. Fixtures live in sandbox-broker/src/evaluator.mjs.

    private static Lesson RustStackHeapLayout(Lesson lesson) => lesson with
    {
        Title = "Stack, heap, and memory layout",
        Subtitle = "See where a Rust value actually lives, how big it is, and why a growing Vec moves its data, before learning the rules that keep that memory safe.",
        Concept = "A running program has two main places for data. The stack holds each function's local values in fixed-size slots that are reclaimed automatically when the function returns. The heap holds data whose size or lifetime isn't known in advance, requested from an allocator and handed back later. A `String` or `Vec` is a small fixed-size descriptor on the stack pointing at its contents on the heap.",
        Body = """
            ## Why this comes before ownership
            Rust's ownership rules exist to answer one question: *when is it safe to use, and to free, a piece of memory?* Those rules make much more sense once you can picture the memory they protect. This lesson builds that picture. Nothing here is unique to Rust; C, C++, Go and the runtime underneath C# and Java all work this way. Rust simply lets you see it and control it.

            ## Memory is numbered bytes
            To a program, memory is a very long row of bytes, each with a number called its **address**. A value of type `u64` occupies 8 consecutive bytes; its address is the number of the first one. A **pointer** or **reference** is a value that holds an address, so code can find data stored elsewhere. On a 64-bit machine an address is 8 bytes.

            ## The stack
            Every time a function is called, the program reserves a block of memory called a **stack frame** for that call's local variables and bookkeeping. When the function returns, its frame is released, and the next call reuses the same space. Frames pile up and come off in strict last-in, first-out order, like a stack of plates, which gives the region its name.

            What makes the stack fast and simple:

            - Reserving and releasing a frame is just moving one pointer up or down. There is no searching for free space.
            - The size of every frame is known when the program is compiled, because the compiler knows the type, and therefore the size, of every local variable.
            - Cleanup is automatic and instant: when the function returns, its locals are gone.

            Its limits follow from the same design. Everything on the stack must have a size known at compile time, and the stack is small: typically 8 MB for a program's main thread on Linux and 2 MB for each thread Rust spawns. A recursive function that calls itself too deeply, or a huge local array like `[u8; 50_000_000]`, exhausts it, and the program stops with a **stack overflow**. And a value on the stack cannot outlive the function that created it, because its frame is reused as soon as the function returns.

            ## The heap
            The **heap** is a large pool of memory managed by an **allocator**: a piece of library code that, when asked for a block of a certain size, finds free space, records that it's in use, and returns its address. Later the program must give the block back (**free** it) so it can be reused.

            The heap is what you use when:

            - the size isn't known until the program runs, such as text read from a file or a list that grows as users sign up;
            - the data must outlive the function that created it;
            - the data is too large for the stack.

            It costs more than the stack: each allocation is a call into the allocator, and data spread around the heap is slower for the processor to read than data packed together. The hard part, though, is **freeing**. Free a block too early and code still holding its address reads memory that now belongs to something else (a **use-after-free**). Free it twice and the allocator's records are corrupted (a **double free**). Never free it and memory use grows forever (a **leak**). In C these are the programmer's responsibility and are among the most common causes of crashes and security vulnerabilities. Garbage-collected languages like C# avoid them by having a runtime find and free unreachable objects periodically. Rust avoids them a third way, at compile time, with ownership: the next lesson.

            ## What Rust puts where
            Local variables live in the function's stack frame. A type whose size is fixed lives *entirely* there: integers, `bool`, `char`, tuples and arrays of those, and structs made of them.

            Types that can grow keep a small, fixed-size **descriptor** in the frame and their contents on the heap. A `Vec<u64>` is three 8-byte numbers:

            - a **pointer** to the heap block holding the elements,
            - its **capacity**: how many elements the block has room for,
            - its **length**: how many elements are actually in use.

            So `std::mem::size_of::<Vec<u64>>()` is 24 on a 64-bit machine, whether the vector holds zero elements or a million. The elements themselves take `capacity * 8` bytes on the heap. A `String` has exactly the same three-part shape (it is a `Vec<u8>` that guarantees valid UTF-8). A `Box<T>` puts one `T` on the heap and is just a pointer: 8 bytes.

            Borrowed views are small too. A `&str` or `&[u64]` is a **fat pointer**: an address plus a length, 16 bytes, pointing into data someone else owns.

            ```rust
            use std::mem::size_of;

            println!("{}", size_of::<u64>());          // 8
            println!("{}", size_of::<Vec<u64>>());     // 24: pointer, capacity, length
            println!("{}", size_of::<String>());       // 24: same shape
            println!("{}", size_of::<&str>());         // 16: pointer and length
            println!("{}", size_of::<Box<u64>>());     // 8: just a pointer
            println!("{}", size_of::<Option<Box<u64>>>()); // 8: None uses the null address
            ```

            The last line shows a Rust optimization: a `Box` is never null, so `Option` can use the null address to mean `None` and needs no extra space.

            ## How a Vec grows
            When you `push` onto a full `Vec`, there is no room in its heap block, and the bytes right after the block may belong to something else. So the vector:

            1. asks the allocator for a new, larger block (Rust's `Vec` roughly doubles its capacity, starting from a small minimum),
            2. copies every element into the new block,
            3. frees the old block,
            4. updates its pointer and capacity.

            Copying everything sounds expensive, but because capacity doubles, it happens rarely: pushing a million elements triggers only about 20 reallocations. Averaged over all the pushes, each one costs a small constant amount of work. When you know roughly how many elements are coming, `Vec::with_capacity(n)` allocates once up front and avoids the copies entirely.

            **Keep step 3 in mind.** After a reallocation, the old block is freed and the elements live at a new address. Any reference that still pointed into the old block would point at freed memory. That is exactly the bug the borrowing lesson shows Rust refusing to compile.

            You can watch this happen: print `values.capacity()` and `values.as_ptr()` after each push, and you'll see the capacity jump and the address change at the same moment.

            ## Alignment and padding
            Processors read memory fastest, and on some hardware only correctly, when a value starts at an address that is a multiple of its size. That requirement is the type's **alignment**: a `u64` must start at a multiple of 8, a `u32` at a multiple of 4, a `u8` anywhere. `std::mem::align_of::<T>()` reports it.

            A struct is aligned like its most demanding field, and its fields must each be aligned too. When a small field is followed by a large one, the compiler inserts unused **padding** bytes so the large one starts on a valid address. The struct's total size is also rounded up to a multiple of its alignment, so that in an array, every element starts aligned.

            Rust's default layout is free to **reorder fields** to minimize padding, so you usually don't have to think about this. But when the layout must be exact, because the struct is shared with C code, written byte-for-byte to a file, or sent over a network, you mark it `#[repr(C)]`. That tells the compiler: keep the fields in the order I wrote them, with C's padding rules. Now field order is your job:

            ```rust
            #[repr(C)]
            struct Wasteful {
                active: bool,   // 1 byte, then 7 bytes of padding so `id` is aligned
                id: u64,        // 8 bytes
                flags: u8,      // 1 byte, then 7 bytes of padding
                timestamp: u64, // 8 bytes
            }                   // 32 bytes, 14 of them wasted

            #[repr(C)]
            struct Compact {
                id: u64,
                timestamp: u64,
                active: bool,
                flags: u8,      // 18 bytes of data, rounded up to 24
            }
            ```

            The rule of thumb for `#[repr(C)]` structs: order fields from largest alignment to smallest. Across millions of records, or in a cache-sensitive loop, the difference is real memory and real speed.

            ## Common mistakes
            - Thinking "stack is for values, heap is for objects". In Rust, a `Vec` value is on the stack and its contents are on the heap; where data lives depends on the type, not on a value/reference split.
            - Assuming the size or field order of a struct without `#[repr(C)]`. The default layout is unspecified and may change between compiler versions.
            - Building a `Vec` one push at a time in a hot loop when the final size is known.
            - Very deep recursion on large inputs; a loop with an explicit `Vec` as a work list cannot overflow the stack.

            ## Try it locally
            1. Print `size_of` for the types in this lesson, then for a struct of your own with and without `#[repr(C)]`.
            2. Push 40 values onto an empty `Vec<u64>`, printing `len`, `capacity` and `as_ptr()` each time. Note where the address changes.
            3. Write a recursive function with no stopping condition and run it to see a stack overflow, then add the condition back.
            """,
        Example = """
            use std::mem::{align_of, size_of};

            #[repr(C)]
            struct Compact {
                id: u64,      // largest alignment first
                timestamp: u64,
                active: bool,
                flags: u8,
            }

            let mut values: Vec<u64> = Vec::new();
            for i in 0..10 {
                values.push(i);
                println!("len {} capacity {} at {:p}", values.len(), values.capacity(), values.as_ptr());
            }
            println!("{} {}", size_of::<Compact>(), align_of::<Compact>()); // 24 8
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Measure layout and growth",
            "Shrink a `#[repr(C)]` struct by ordering its fields, record how a `Vec` grows, and report a vector's stack and heap footprint. The grader runs on a 64-bit machine. Don't write a `main` function.",
            [
                "`#[repr(C)] struct SensorReading` with fields `id: u64`, `timestamp: u64`, `celsius: f32`, `active: bool` and `flags: u8`, ordered so the struct is 24 bytes",
                "`fn growth_steps(n: usize) -> Vec<usize>`: push `n` values onto a new, empty `Vec<u64>` and return each new capacity, in order, every time the capacity changes",
                "`fn vec_footprint(values: &Vec<u64>) -> (usize, usize)` returns the size of the vector's descriptor and the number of bytes its heap block reserves (capacity, not length, times the element size)",
            ],
            """
            #[repr(C)]
            struct SensorReading {
                active: bool,
                id: u64,
                flags: u8,
                timestamp: u64,
                celsius: f32,
            }

            fn growth_steps(n: usize) -> Vec<usize> {
                todo!()
            }

            fn vec_footprint(values: &Vec<u64>) -> (usize, usize) {
                todo!()
            }
            """,
            null,
            [],
            "Put the two `u64` fields first, then the `f32`, then the one-byte fields. For growth, remember the capacity before each `push` and record `values.capacity()` whenever it changed. The footprint is `(std::mem::size_of::<Vec<u64>>(), values.capacity() * std::mem::size_of::<u64>())`.",
            ["The struct has no wasted padding", "Each reallocation is recorded", "Stack and heap sizes are reported separately"]),
    };

    private static Lesson RustOwnership(Lesson lesson) => lesson with
    {
        Title = "Ownership, moves, and Drop",
        Subtitle = "Every value has exactly one owner, and the owner's scope decides when it's cleaned up. That one rule replaces both manual `free` and the garbage collector.",
        Concept = "Each value has one owner. Assigning it or passing it by value moves ownership, which copies only the small descriptor and leaves the old name unusable. When the owner goes out of scope, Rust runs the value's cleanup (`Drop`) exactly once, freeing heap memory and closing files or sockets at a predictable moment.",
        Body = """
            ## The rules
            The previous lesson ended with the hard question of heap memory: who frees it, and when? Rust answers with three rules, checked entirely at compile time:

            1. Every value has exactly one **owner**: a variable, a struct field, an element of a collection.
            2. Ownership can be **moved** to a new owner, after which the old one can no longer be used.
            3. When the owner goes out of **scope** (the end of the `{ ... }` block it was declared in), the value is **dropped**: its cleanup runs, and its memory is released.

            Since only one owner exists at a time, and the compiler knows exactly where each owner's scope ends, every heap block is freed exactly once, at a known point, with no runtime work to figure it out. No double free, no forgetting, and no garbage collector pausing the program.

            ## Moves
            ```rust
            let first = String::from("Ada");
            let second = first;          // ownership moves to `second`
            println!("{first}");         // error[E0382]: borrow of moved value: `first`
            ```

            Picture the memory. `first` is a 24-byte descriptor on the stack (pointer, capacity, length) pointing at the bytes `Ada` on the heap. `let second = first;` copies those 24 bytes into `second`. Now two descriptors point at the same heap block. If both were usable, both would try to free it at the end of the scope: a double free. Rust's answer is to treat the old one as **moved out of**: it still sits in the frame, but the compiler forbids any use of it and won't drop it. Only `second` frees the text.

            Notice what a move does *not* do: it never copies the heap data. Moving a `String` with a million characters copies 24 bytes. That is why moves are cheap, and why this lesson's exercise can check that a function hands back the very same allocation it received.

            Passing a value to a function is a move, and so is returning one:

            ```rust
            fn consume(name: String) -> usize {
                name.len()
            }   // `name` goes out of scope here: the text is freed

            let title = String::from("Systems");
            let length = consume(title);   // `title` moved into the function
            // `title` can't be used any more
            ```

            A function that takes a value by value (`name: String`, no `&`) is announcing in its signature: *I'm taking responsibility for this.* A function that returns a value is handing responsibility back.

            ## Copy types
            Some types are cheap and safe to duplicate bit-for-bit because they own no heap memory or other resources: integers, floats, `bool`, `char`, shared references (`&T`), and tuples or arrays made only of those. These types implement the **`Copy`** trait, and assigning them copies instead of moving, so the original stays usable:

            ```rust
            let a: u32 = 5;
            let b = a;     // a copy
            println!("{a} {b}");   // fine
            ```

            A struct can opt in with `#[derive(Clone, Copy)]` when all its fields are `Copy`. A type that owns heap data or implements `Drop` can never be `Copy`, because two bit-identical copies would both try to clean up.

            ## Clone: an explicit, possibly expensive copy
            When you really need two independent values, call `.clone()`. For a `String` that allocates a new heap block and copies every byte. Rust makes you write it so the cost is visible in the code. `clone()` is fine when you mean it; a function full of `clone()` calls added only to silence errors usually means its signatures should borrow instead (next lesson).

            ## Drop: cleanup at the end of scope
            Dropping a value runs its **destructor**. For a `String` or `Vec` that frees the heap block. But a destructor can do anything, which turns ownership into a general tool for managing *resources*:

            - A `File` closes its file descriptor when dropped.
            - A `TcpStream` closes its network connection.
            - A `MutexGuard` unlocks the mutex.
            - A temporary directory deletes itself.

            This pattern is called **RAII** ("resource acquisition is initialization"): acquire the resource when you create the value, release it when the value goes away. In C# you'd write `using var file = ...;` and hope everyone remembers to. In Rust it's the default for every type, and you cannot forget.

            You can write your own destructor by implementing the `Drop` trait:

            ```rust
            struct Lease {
                name: String,
            }

            impl Drop for Lease {
                fn drop(&mut self) {
                    println!("releasing {}", self.name);
                }
            }
            ```

            You never call `drop` methods yourself; Rust calls them. The order is predictable:

            - Local variables are dropped at the end of their scope **in reverse order of declaration**: the last one created is the first one cleaned up, so later values that depend on earlier ones go first.
            - A struct's fields are dropped after the struct's own `drop` runs, in declaration order.
            - A value that was moved away isn't dropped where it *was*; it's dropped wherever its *new* owner's scope ends.
            - To drop something early, pass it to `std::mem::drop(value)` (also available as just `drop`). That function simply takes ownership and does nothing, so the value is dropped as the function returns.

            ## Functions that move in and out
            Because moving is cheap, a function can take ownership of a value, change it, and hand it back, without ever copying the heap data:

            ```rust
            fn shout(mut text: String) -> String {
                text.make_ascii_uppercase(); // changes the bytes in place
                text                         // returns the same allocation
            }
            ```

            Compare `text.to_uppercase()`, which builds a *new* `String` and lets the old one be dropped: correct, but it allocates. (`to_uppercase` handles all of Unicode, which can change the length of the text; `make_ascii_uppercase` only changes ASCII letters, so it can work in place.) Choosing between "transform in place" and "build new" is a routine decision in systems code, and ownership makes it visible in the signature.

            A function can also choose which of two values to keep: `fn keep_longest(a: Lease, b: Lease) -> Lease` takes both, returns one, and the other is dropped when the function ends.

            ## Common mistakes
            - Sprinkling `.clone()` to get past E0382 instead of asking whether the function should borrow.
            - Expecting a value to still be usable after passing it to a function that takes it by value.
            - Assuming a move copies data and avoiding moves "for performance". A move copies only the descriptor.
            - Holding a resource (a lock, a file, a connection) in a variable that lives far longer than needed. Put it in a smaller block, or `drop` it explicitly.

            ## Try it locally
            1. Create the `Lease` type with a `Drop` that prints its name. Make three leases in `main` and predict the order of the messages before running.
            2. Pass one lease to a function that takes it by value, and see its message print when that function returns.
            3. Print `text.as_ptr()` before and after `shout`, then change `shout` to use `to_uppercase` and compare.
            4. Try to use a moved `String` and read the whole E0382 message, including where it says the move happened.
            """,
        Example = """
            struct Lease {
                name: String,
            }

            impl Drop for Lease {
                fn drop(&mut self) {
                    println!("releasing {}", self.name);
                }
            }

            fn hand_over(lease: Lease) -> usize {
                lease.name.len()
            } // the lease is dropped here: "releasing ..." prints

            fn main() {
                let a = Lease { name: String::from("alpha") };
                let b = Lease { name: String::from("beta") };
                hand_over(a);   // a moves into the function
            } // b is dropped here
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Track ownership with Drop",
            "Implement `Drop` so the grader can watch exactly when each `Lease` is cleaned up, then write functions that take, choose between, and hand back owned values. The grader provides `fn record_drop(name: &str)`; don't define it, and don't write a `main` function.",
            [
                "`struct Lease { name: String }` with an `impl Drop for Lease` that calls `record_drop(&self.name)`",
                "`fn hand_over(lease: Lease) -> usize` takes ownership and returns the name's length; the lease must be dropped when the function returns",
                "`fn keep_longest(a: Lease, b: Lease) -> Lease` returns the lease with the longer name (`a` on a tie); the other one is dropped before returning",
                "`fn shout(name: String) -> String` returns the text in ASCII uppercase **using the same allocation** it was given, without building a new `String`",
            ],
            """
            // The grader provides: fn record_drop(name: &str)

            struct Lease {
                name: String,
            }

            // impl Drop for Lease { ... }

            fn hand_over(lease: Lease) -> usize {
                todo!()
            }

            fn keep_longest(a: Lease, b: Lease) -> Lease {
                todo!()
            }

            fn shout(name: String) -> String {
                todo!()
            }
            """,
            null,
            [],
            "`impl Drop for Lease { fn drop(&mut self) { record_drop(&self.name); } }`. Ownership does the rest: `hand_over` drops its lease at its closing brace, and `keep_longest` drops whichever value it doesn't return. For `shout`, make the parameter `mut name: String`, call `name.make_ascii_uppercase()`, and return `name`; `to_uppercase()` would allocate a new String.",
            ["Drop runs at the end of each scope, in reverse order", "A function that takes ownership drops the value", "Only the returned lease survives", "The returned String reuses its allocation"]),
    };

    private static Lesson RustBorrowingReferences(Lesson lesson) => lesson with
    {
        Title = "Borrowing: shared and mutable references",
        Subtitle = "Lend a value without giving it away, and learn the one rule, many readers or one writer, that rules out dangling pointers and data races at compile time.",
        Concept = "A reference borrows a value without taking ownership. You may have any number of shared references (`&T`) to a value, or exactly one mutable reference (`&mut T`), but never both at the same time, and no reference may outlive the value it points to. Those two rules are what make every reference in safe Rust valid.",
        Body = """
            ## Why borrowing exists
            If every function took ownership of its arguments, you'd constantly be passing values in and returning them back out just to keep using them. Most functions only need to *look at* a value, or *change* it in place, then give it back. That's what a **reference** is for: it lets code use a value while ownership stays where it was. Creating a reference is called **borrowing**.

            ```rust
            fn total_len(names: &[String]) -> usize {
                names.iter().map(|name| name.len()).sum()
            }

            let names = vec![String::from("ada"), String::from("grace")];
            let total = total_len(&names);   // lend the list
            println!("{} names, {total} letters", names.len()); // still ours
            ```

            Under the hood a reference is just an address (plus a length, for slices and `&str`). What's different from a C pointer is the set of rules the compiler checks about it.

            ## Two kinds of reference
            - **`&T`, a shared reference**: read-only access. Any number can exist at once, because many readers can't interfere with each other.
            - **`&mut T`, a mutable reference**: read-and-write access. While one exists, it is the *only* way to reach that value: no other references of either kind.

            ```rust
            fn add_suffix(names: &mut [String], suffix: &str) {
                for name in names.iter_mut() {
                    name.push_str(suffix);
                }
            }

            let mut names = vec![String::from("ada")];
            add_suffix(&mut names, "!");
            ```

            Both the binding (`let mut names`) and the borrow (`&mut names`) must say `mut`. Mutability is visible at every step.

            A useful way to remember the rule: `&` means **shared**, and `&mut` means **exclusive**. Exclusive access is what makes mutation safe.

            ## The rule, and the bug it prevents
            **At any moment, a value can have either any number of `&` references or exactly one `&mut` reference.**

            Here is the classic bug this rule catches. Recall from the stack-and-heap lesson what `push` does when a `Vec` is full: it moves every element to a new heap block and frees the old one.

            ```rust
            let mut values = vec![4, 8];
            let first = &values[0];   // a reference into the heap block
            values.push(15);          // may move the elements and free that block
            println!("{first}");      // would read freed memory
            ```

            Rust refuses: **E0502: cannot borrow `values` as mutable because it is also borrowed as immutable**. `push` needs `&mut values`, and `first` is still a live `&` borrow. In C++ the equivalent code compiles, and works most of the time, then reads garbage the one time the push reallocates. This class of bug is called **iterator invalidation**, and it's a common source of security holes.

            The same rule prevents **data races**. A data race is two threads touching the same memory at the same time, at least one of them writing, without coordination. That requires two simultaneous access paths with at least one able to write, which is exactly what "one `&mut` or many `&`" forbids. The concurrency lesson builds on this.

            ## A borrow lasts until its last use
            The compiler measures a borrow from where it's created to the **last place it's used**, not to the end of the block. So the fix above is often just reordering, or copying out the value you need:

            ```rust
            let first = values[0];   // copy the i32 out; no borrow remains
            values.push(15);
            println!("{first}");     // fine
            ```

            Or simply use the borrow before you mutate:

            ```rust
            let first = &values[0];
            println!("{first}");     // last use of the borrow
            values.push(15);         // fine: the borrow already ended
            ```

            When you hit E0502 or **E0499** (two mutable borrows), ask: *which borrow is still alive, and does it need to be?*

            ## References can't outlive their data
            The other half of the rules: a reference must never point at a value that's already been dropped. That's what rules out the C bug of returning the address of a local variable:

            ```rust
            fn dangling() -> &String {
                let text = String::from("gone");
                &text
            }   // `text` is dropped here, so the reference would dangle
            ```

            This doesn't compile. The fix is to return the `String` itself, moving ownership to the caller. A function can only return a reference into something the *caller* gave it, which is exactly what the next lesson, on lifetimes, makes precise.

            ## Slices are borrows
            `&str` and `&[T]` are borrowed views into part of a string or array: an address plus a length. `&text[0..5]` borrows five bytes of `text` without copying them. That's why functions should take `&str` and `&[T]` parameters: they accept a whole `String` or `Vec`, an array, or a piece of one, with no copying.

            A function can return a slice of its input:

            ```rust
            fn longest_word(text: &str) -> &str {
                let mut best = "";
                for word in text.split_whitespace() {
                    if word.len() > best.len() {
                        best = word;
                    }
                }
                best
            }
            ```

            The returned `&str` points into the caller's text. No new string is created: the word is never copied. As long as the caller keeps `text` alive and unchanged, the word stays valid, and the compiler checks that.

            ## Three ways to loop over a collection
            Ownership and borrowing show up directly in `for` loops:

            - `for name in &names` (or `names.iter()`): each item is a `&String`. The list is only read.
            - `for name in &mut names` (or `names.iter_mut()`): each item is a `&mut String` you can change in place.
            - `for name in names` (or `names.into_iter()`): each item is an owned `String`. The loop **consumes** the list, which can't be used afterwards.

            ## Small conveniences that hide borrows
            - **Method calls borrow automatically.** `names.len()` borrows `names` as `&`; `names.push(x)` borrows it as `&mut`. You don't write `(&names).len()`.
            - **Auto-deref.** Calling a method through a reference finds the method on the value it points to. `&String` also converts automatically to `&str` where one is expected.
            - **`*` dereferences** when you need the value itself: `*count += 1` through a `&mut u32`.

            ## Common mistakes
            - Holding a reference into a collection across a call that modifies the collection.
            - Taking `&mut` when `&` would do, which needlessly blocks every other use of the value.
            - Writing `fn f(v: &Vec<String>)` instead of `fn f(v: &[String])`, which accepts fewer kinds of caller.
            - Fighting a borrow error with `.clone()` before checking whether reordering two lines fixes it.

            ## Try it locally
            1. Write the reallocation example above and read the full E0502 message. Fix it two ways: by copying the value out, and by moving the `println!` up.
            2. Create two `&mut` borrows of one variable and use both; read E0499.
            3. Write the `dangling` function and read the compiler's suggestion.
            4. Loop over a `Vec<String>` all three ways and try to use the vector after each loop.
            """,
        Example = """
            fn first_and_push(values: &mut Vec<i32>, extra: i32) -> i32 {
                let first = values[0]; // copy out; no borrow is held
                values.push(extra);    // may reallocate safely
                first
            }

            fn longest_word(text: &str) -> &str {
                text.split_whitespace().fold("", |best, w| if w.len() > best.len() { w } else { best })
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Lend, change in place, and fix a borrow conflict",
            "Write four functions that borrow instead of taking ownership. The starter's `first_and_push` doesn't compile; fix it. The grader keeps using its values after calling you, so a function that takes ownership won't compile. Don't write a `main` function.",
            [
                "`fn total_len(names: &[String]) -> usize` returns the total length of all names",
                "`fn add_suffix(names: &mut [String], suffix: &str)` appends the suffix to every name in place",
                "`fn first_and_push(values: &mut Vec<i32>, extra: i32) -> i32` pushes `extra` and returns the element that was first (the vector is never empty)",
                "`fn longest_word(text: &str) -> &str` returns the longest whitespace-separated word as a slice of `text`, the first one on a tie, or `\"\"` when there are none",
            ],
            """
            fn total_len(names: &[String]) -> usize {
                todo!()
            }

            fn add_suffix(names: &mut [String], suffix: &str) {
                todo!()
            }

            fn first_and_push(values: &mut Vec<i32>, extra: i32) -> i32 {
                let first = &values[0];
                values.push(extra);
                *first
            }

            fn longest_word(text: &str) -> &str {
                todo!()
            }
            """,
            null,
            [],
            "Read the E0502 error for `first_and_push`: the shared borrow `first` is still alive when `push` needs a mutable one. Copy the `i32` out with `let first = values[0];` instead of borrowing it. For `add_suffix`, loop with `names.iter_mut()` and `push_str`. For `longest_word`, only replace the best word when a new one is strictly longer, so the first wins ties.",
            ["Shared borrows leave the caller's data usable", "A mutable borrow changes data in place", "The borrow conflict is fixed without changing behavior", "The longest word borrows from the text"]),
    };

    private static Lesson RustLifetimes(Lesson lesson) => lesson with
    {
        Title = "Lifetimes: relationships between references",
        Subtitle = "A lifetime annotation doesn't make anything live longer. It tells the compiler which input a returned or stored reference points into, so it can check that the data outlives the reference.",
        Concept = "Every reference is valid only while the data it points at exists, and the compiler calls that span its lifetime. Usually it works lifetimes out alone. When a function returns a reference and it's ambiguous which input it came from, or a struct stores a reference, you name the relationship with a lifetime parameter such as `'a`.",
        Body = """
            ## The question lifetimes answer
            The borrowing lesson established that a reference must never outlive the value it points to. Within one function the compiler can see that directly. Across a function call it can't look inside the function, only at its **signature**, so the signature has to say how its output references relate to its input references. That's all a lifetime annotation is.

            Consider:

            ```rust
            fn longer(a: &str, b: &str) -> &str {
                if b.len() > a.len() { b } else { a }
            }
            ```

            This fails with **E0106: missing lifetime specifier**. The returned `&str` points into either `a` or `b`, and the caller needs to know which, because that decides how long the caller may keep using the result. If it came from `a`, it's valid as long as `a`'s data lives; if from `b`, as long as `b`'s.

            ## Naming the relationship
            ```rust
            fn longer<'a>(a: &'a str, b: &'a str) -> &'a str {
                if b.len() > a.len() { b } else { a }
            }
            ```

            Read `'a` (say "tick a") as a label for *some span of the program*. `<'a>` declares the label, like a generic type parameter. The signature then says: "there is a span `'a` during which both `a` and `b` are valid, and the result is valid for that span." When you call it, the compiler picks the largest span that fits both arguments, which is the *shorter* of the two, and checks that you don't use the result beyond it:

            ```rust
            let outer = String::from("a longer string");
            let result;
            {
                let inner = String::from("short");
                result = longer(&outer, &inner);
            }   // `inner` is dropped here
            println!("{result}");   // error[E0597]: `inner` does not live long enough
            ```

            Even though, at runtime, `result` would point at `outer`, the signature promised only "valid while both are", and the compiler holds you to the signature. Fix it by using the result inside the inner block, or by converting it to an owned `String` before `inner` goes away.

            Two important points:

            - **Annotations don't change how long anything lives.** Values are dropped when their owners go out of scope, exactly as before. Lifetimes only describe relationships so the compiler can check them.
            - **They're names, not durations.** `'a` doesn't mean "a long time"; it means "the same span as these other references".

            ## Elision: why you rarely write them
            Most functions return references in one obvious way, so the compiler applies three **elision rules** and fills the lifetimes in for you:

            1. Each reference parameter gets its own lifetime.
            2. If there is exactly **one** reference parameter, every output reference gets its lifetime. So `fn first_line(text: &str) -> &str` needs no annotation: the result can only have come from `text`.
            3. If one of the parameters is `&self` or `&mut self` (a method), output references get **self's** lifetime.

            When those rules don't settle the question, as with `longer`'s two inputs, the compiler asks you. Rule 3 is the one that most often surprises people, as the struct example below shows.

            ## Structs that hold references
            A struct can store a reference instead of owned data, which avoids copying. But then the struct must not outlive the data, so its type carries a lifetime parameter:

            ```rust
            struct Words<'a> {
                text: &'a str,   // borrowed from someone else
                pos: usize,
            }
            ```

            `Words<'a>` reads as "a `Words` that borrows text valid for `'a`". A `Words` can be created, used and dropped while the text exists, and the compiler stops any `Words` from escaping past it.

            Now the subtle part. A method that returns the next word:

            ```rust
            impl<'a> Words<'a> {
                fn next_word(&mut self) -> Option<&str> { /* ... */ }
            }
            ```

            By elision rule 3, the returned `&str` is tied to `&mut self`: the *borrow of the `Words` value*, not the text. That signature claims each word is only valid while that particular mutable borrow lasts. The consequences are severe: you can't keep one word while asking for the next (that needs a second `&mut self` while the first word still holds the first one, which is **E0499**), and no word can be used after the `Words` value is dropped.

            But the words really point into the original text, which lives longer than the `Words` value. Say so:

            ```rust
            impl<'a> Words<'a> {
                fn next_word(&mut self) -> Option<&'a str> { /* ... */ }
            }
            ```

            Now each word is tied to `'a`, the text, and stays valid after the `Words` is gone. This is the lifetime pattern behind every borrowing parser and iterator: **return references tied to the data, not to the cursor walking over it.**

            ## 'static
            `'static` is the one built-in lifetime name: valid for the entire run of the program. String literals are `&'static str`, because their bytes are stored inside the program file itself and never go away. You'll also see `T: 'static` as a requirement, for example on threads, meaning "this type contains no borrowed data that could expire". That's satisfied by owned types like `String`, not only by literals.

            When you meet a lifetime error, adding `'static` is almost never the fix. It demands that data live forever, which usually just moves the error somewhere else.

            ## When to stop borrowing
            Borrowing avoids copies, but every borrowed field adds a lifetime parameter that spreads to everything that holds the struct. Long-lived structs (configuration, application state, anything stored in a collection or sent between threads) are usually simpler with **owned** fields like `String`. Borrowing shines in short-lived, performance-sensitive code: parsers, iterators, views over a buffer. Choosing owned data on purpose is a design decision, not a defeat.

            ## Error codes you'll see
            - **E0106 missing lifetime specifier**: a signature returns or stores a reference and elision can't decide where it comes from.
            - **E0597 ... does not live long enough**: a value is dropped while something still borrows it.
            - **E0515 cannot return reference to local variable**: return the owned value instead.
            - **E0499 / E0502**: often a returned reference accidentally tied to `&mut self`.

            ## Common mistakes
            - Thinking `'a` extends how long data lives.
            - Leaving a method's return type elided when the result really borrows from a field's data, not from `self`.
            - Reaching for `'static` to silence an error.
            - Storing references in long-lived structs where owned data would be simpler.

            ## Try it locally
            1. Write `longer` without annotations and read E0106, including its suggestion.
            2. Reproduce the E0597 example, then fix it two ways.
            3. Build `Words` with `-> Option<&str>`, try to keep two words at once, and read the error. Change it to `Option<&'a str>` and watch the error disappear.
            """,
        Example = """
            fn longer<'a>(a: &'a str, b: &'a str) -> &'a str {
                if b.len() > a.len() { b } else { a }
            }

            struct Words<'a> {
                text: &'a str,
                pos: usize,
            }

            impl<'a> Words<'a> {
                // Tied to the text ('a), not to the &mut borrow of self.
                fn next_word(&mut self) -> Option<&'a str> {
                    let rest = &self.text[self.pos..];
                    let start = rest.find(|c: char| !c.is_whitespace())?;
                    let word = rest[start..].split_whitespace().next()?;
                    self.pos += start + word.len();
                    Some(word)
                }
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Return references that outlive the cursor",
            "Write two functions and a word iterator over borrowed text. The grader keeps words after the iterator is gone and holds several words at once, so the lifetimes in your signatures must describe where each reference really points. Don't write a `main` function.",
            [
                "`fn longer<'a>(a: &'a str, b: &'a str) -> &'a str` returns the longer string, or `a` on a tie",
                "`fn first_line(text: &str) -> &str` returns the text up to the first newline (the whole text if there is none, `\"\"` if it's empty)",
                "`struct Words<'a>` with `fn new(text: &'a str) -> Words<'a>` and `fn next_word(&mut self) -> Option<&'a str>` that returns each whitespace-separated word in turn as a slice of the original text, then `None`",
            ],
            """
            fn longer<'a>(a: &'a str, b: &'a str) -> &'a str {
                todo!()
            }

            fn first_line(text: &str) -> &str {
                todo!()
            }

            struct Words<'a> {
                text: &'a str,
                pos: usize,
            }

            impl<'a> Words<'a> {
                fn new(text: &'a str) -> Words<'a> {
                    todo!()
                }

                fn next_word(&mut self) -> Option<&'a str> {
                    todo!()
                }
            }
            """,
            null,
            [],
            "Keep the `'a` in `next_word`'s return type: it ties each word to the text, not to `&mut self`. Track your position with `pos`: skip whitespace with `find(|c: char| !c.is_whitespace())`, take the word up to the next whitespace, and advance `pos` past it. `text.lines().next().unwrap_or(\"\")` gives the first line. You may also store a `SplitWhitespace<'a>` instead of `pos`.",
            ["The longer string keeps the shorter lifetime's rules", "First line slices the input", "Words outlive the iterator that produced them"]),
    };
}
