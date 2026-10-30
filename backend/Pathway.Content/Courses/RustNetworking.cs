namespace Pathway.Content;

static partial class Curriculum
{
    // Authored lessons for the "Networking from sockets up" module: raw TCP with std::net, message
    // framing, concurrent servers with timeouts, and a Tokio key-value server. These sit before the
    // Axum lessons so HTTP frameworks are built on understood foundations. Fixtures live in
    // sandbox-broker/src/evaluator.mjs; every exercise talks to itself over 127.0.0.1.

    private static Lesson RustNetTcpBasics(Lesson lesson) => lesson with
    {
        Subtitle = "Open a listening socket, accept a connection, and exchange bytes with std::net, understanding exactly what TCP does and doesn't promise.",
        Concept = "A socket is the operating system's handle for one end of a network conversation, identified by an IP address and a port. TCP turns unreliable packets into a reliable, ordered stream of bytes between two sockets, but it is only a stream: it doesn't preserve how the bytes were split into writes, and each side signals \"I'm done sending\" by closing or shutting down its half.",
        Body = """
            ## Addresses and ports
            Every machine on a network has one or more **IP addresses**: four numbers like `192.168.1.20` for IPv4, or a longer form like `2001:db8::1` for IPv6. An address identifies a machine; a **port**, a number from 1 to 65535, identifies one conversation endpoint *on* that machine, so many programs can use the network at once. A web server usually listens on port 443, SSH on 22, a development server on something like 8080. In Rust, an address plus a port is a `SocketAddr`.

            Two addresses matter constantly in development and security:

            - **`127.0.0.1`** (and `::1` in IPv6), called **loopback** or `localhost`, means "this same machine". Traffic never leaves the computer. A server bound to it can only be reached by programs running on the same machine.
            - **`0.0.0.0`** (and `::`), the **wildcard** address, means "every network interface this machine has". A server bound to it can be reached from the local network, and from the internet if a firewall allows it.

            Binding a development server or an admin endpoint to `0.0.0.0` by accident is a real and common way to expose something that was never meant to be public. Default to loopback; listen on all interfaces only on purpose.

            **Port 0** is a special request: "pick any free port for me". Tests use it so they never collide with another program; afterwards `listener.local_addr()` tells you which port the OS actually chose.

            ## What TCP gives you
            Underneath, a network moves **packets**: small, independent chunks that can be lost, duplicated, delayed or delivered out of order. **TCP** (Transmission Control Protocol), implemented inside the kernel, builds something much more convenient on top:

            - A **connection** between exactly two sockets, set up with a short exchange called the three-way handshake.
            - **Reliable, ordered delivery**: bytes arrive exactly once and in the order sent. Lost packets are resent automatically.
            - **Flow control**: a fast sender can't overwhelm a slow receiver; the sender's writes slow down instead.
            - A **byte stream**, with no message boundaries. If one side writes `"hello"` and then `"world"`, the other side might receive `"hel"`, then `"loworld"`, or all ten bytes at once. Nothing in TCP records where one write ended. The next lesson deals with that.

            **UDP**, the other common transport, is the opposite trade: it sends individual **datagrams** that keep their boundaries but may be lost, duplicated or reordered, with no connection and no flow control. DNS, video calls and games use it where speed matters more than perfect delivery. Rust has `std::net::UdpSocket` for it.

            ## A server and a client with std::net
            A server **binds** a listening socket to an address and port, then **accepts** connections one at a time. Each accepted connection is a new `TcpStream`, a socket connected to one particular client, while the listener keeps listening.

            ```rust
            use std::io::{Read, Write};
            use std::net::TcpListener;

            let listener = TcpListener::bind("127.0.0.1:0")?;
            println!("listening on {}", listener.local_addr()?);
            let (mut stream, peer) = listener.accept()?;   // waits for a client
            println!("connection from {peer}");
            let mut buffer = [0u8; 4096];
            let read = stream.read(&mut buffer)?;
            stream.write_all(&buffer[..read])?;
            ```

            A client **connects**:

            ```rust
            use std::net::TcpStream;

            let mut stream = TcpStream::connect("127.0.0.1:8080")?;
            stream.write_all(b"hello")?;
            ```

            `connect` also accepts a host name like `"example.com:443"`; it looks the name up with DNS through the `ToSocketAddrs` trait and tries each address returned.

            `TcpStream` implements `Read` and `Write`, so everything from the standard-streams lesson applies: `read` may return fewer bytes than the buffer holds, `write` may write fewer than you gave it, `write_all` loops until done, and `BufReader` adds line reading. Dropping a `TcpStream` closes the connection; ownership manages sockets exactly as it manages files.

            ## The end of the stream
            `read` returning **0** means the other side has finished sending: end of stream, just like end of file. How does a client say "that's my whole request" while still waiting for the reply? It can't close the socket, because then it couldn't read either. Instead it **shuts down its writing half**:

            ```rust
            use std::net::Shutdown;

            stream.write_all(request)?;
            stream.shutdown(Shutdown::Write)?;   // "I'm done sending", still able to read
            let mut reply = Vec::new();
            stream.read_to_end(&mut reply)?;     // reads until the server closes its side
            ```

            The server's `read` then returns 0, so it knows the request is complete, replies, and closes. This **half-close** is how simple request/response protocols over raw TCP know when a message ends. Forgetting it leaves both sides waiting for each other forever.

            Two failure signals to recognise: **`ConnectionRefused`** from `connect` means nothing is listening on that port; **`ConnectionReset`** means the other side closed abruptly, for example a program that exited with unread data still in its buffer.

            ## Echo: loop until the end
            Because of the byte-stream rule, a server that echoes data back must loop: read whatever has arrived, write it back, and repeat until `read` returns 0. A single `read` call gets *some* of the data, perhaps all of it in a small test, and then silently drops the rest when the data is larger or the network is slower. Bugs like that pass every local test and fail in production.

            Also note the order problem in a simple client that writes its whole request before reading anything. The kernel buffers only a limited amount of data in each direction. If the request is large and the server echoes as it reads, both buffers can fill up: the server blocks writing its echo, so it stops reading, so the client blocks writing its request. Small messages fit in the buffers; large ones need the client to read and write concurrently.

            ## Common mistakes
            - Binding to `0.0.0.0` when only local programs need access.
            - Assuming one `read` returns one complete message.
            - Forgetting `shutdown(Shutdown::Write)` in a protocol that relies on end-of-stream.
            - Hard-coding a port in tests instead of binding to port 0.

            ## Try it locally
            1. Write an echo server on `127.0.0.1:7878` and connect to it with `nc 127.0.0.1 7878` (Linux/macOS) or `telnet`. Type lines and watch them come back.
            2. Start a second `nc` while the first is connected. Notice it isn't served until the first disconnects; the third lesson in this module fixes that.
            3. Bind to `0.0.0.0` and try connecting from another device on your network; then switch back to `127.0.0.1` and try again.
            """,
        Example = """
            use std::io::{Read, Write};
            use std::net::{Shutdown, TcpStream};

            fn request(address: &str, message: &[u8]) -> std::io::Result<Vec<u8>> {
                let mut stream = TcpStream::connect(address)?;
                stream.write_all(message)?;
                stream.shutdown(Shutdown::Write)?; // the server's read now returns 0 at the end
                let mut reply = Vec::new();
                stream.read_to_end(&mut reply)?;   // until the server closes its side
                Ok(reply)
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Echo over a real TCP connection",
            "Write a local listener, a server that echoes one connection, and a client. The grader runs them against each other over 127.0.0.1 and sends 50,000 bytes. Don't write a `main` function.",
            [
                "`fn local_listener() -> io::Result<(TcpListener, SocketAddr)>` binds to loopback only, on a port the OS chooses, and returns the listener with its real address",
                "`fn echo_once(listener: &TcpListener) -> io::Result<usize>` accepts one connection, writes back every byte it receives until the client finishes sending, and returns how many bytes it echoed",
                "`fn request(address: SocketAddr, message: &[u8]) -> io::Result<Vec<u8>>` connects, sends the message, signals that it has finished sending, and returns everything the server sends back",
            ],
            """
            use std::io::{self, Read, Write};
            use std::net::{Shutdown, SocketAddr, TcpListener, TcpStream};

            fn local_listener() -> io::Result<(TcpListener, SocketAddr)> {
                todo!()
            }

            fn echo_once(listener: &TcpListener) -> io::Result<usize> {
                todo!()
            }

            fn request(address: SocketAddr, message: &[u8]) -> io::Result<Vec<u8>> {
                todo!()
            }
            """,
            null,
            [],
            "Bind to `\"127.0.0.1:0\"` and use `local_addr()`. In `echo_once`, loop: `read` into a buffer, stop when it returns 0, otherwise `write_all` that many bytes back and add them to a total. In `request`, `write_all`, then `shutdown(Shutdown::Write)`, then `read_to_end`.",
            ["The listener is local only, on a chosen port", "Every byte is echoed however TCP splits it", "The client half-closes and reads the full reply"]),
    };

    private static Lesson RustNetFraming(Lesson lesson) => lesson with
    {
        Subtitle = "Give a byte stream message boundaries with a length prefix, read it correctly however the bytes arrive, and refuse frames that would exhaust memory.",
        Concept = "Because TCP only delivers a stream of bytes, every protocol has to define where one message ends and the next begins. A length prefix, a fixed-size header saying how many bytes follow, is the most robust way. Reading it correctly means looping until the whole header and payload have arrived, telling a clean end of stream apart from one cut off mid-message, and checking the length before allocating.",
        Body = """
            ## The problem
            The previous lesson ended with a fact that shapes every network protocol: TCP preserves the *order* of bytes but not the *boundaries* between writes. If a client sends two messages, the server may see them glued together, split in the middle, or both. Something in the data itself must say where each message ends. That structure is called **framing**, and each complete message is a **frame**.

            ## Three ways to frame
            - **Delimiters**: end each message with a special byte sequence, typically a newline. Simple and human-readable: HTTP/1.1 headers, Redis, SMTP and most chat-style protocols use lines. The catch is that the delimiter can't appear inside a message unless you invent an escaping scheme, which rules out arbitrary binary data.
            - **Fixed size**: every message is exactly N bytes. Trivial to read, but wasteful and inflexible.
            - **Length prefix**: start each message with a fixed-size number saying how many bytes follow. Works for any payload, including binary, and the reader knows exactly how much to read. Most binary protocols use it, including HTTP/2, PostgreSQL's wire protocol, Kafka and gRPC.

            ## A length-prefixed format
            A common layout is a 4-byte unsigned length, then that many payload bytes:

            ```text
            [ 0x00 0x00 0x00 0x05 ][ h e l l o ][ 0x00 0x00 0x00 0x00 ][ 0x00 0x00 0x01 0x2C ][ 300 bytes ... ]
              length = 5            payload       length = 0 (empty)     length = 300
            ```

            The length has to be written in an agreed **byte order**. Network protocols conventionally use **big-endian**, most significant byte first, often called "network byte order". The bytes-and-endianness lesson covers this; in Rust it's `u32::to_be_bytes` to write and `u32::from_be_bytes` to read. Picking the machine's native order (`to_ne_bytes`) is a classic bug that only appears when a different kind of computer joins the conversation.

            Writing a frame is short:

            ```rust
            fn write_frame<W: Write>(writer: &mut W, payload: &[u8]) -> io::Result<()> {
                let length = u32::try_from(payload.len())
                    .map_err(|_| io::Error::new(io::ErrorKind::InvalidInput, "frame too large"))?;
                writer.write_all(&length.to_be_bytes())?;
                writer.write_all(payload)
            }
            ```

            `try_from` refuses payloads over 4 GB instead of silently truncating the length with `as`, which would corrupt the stream for everything after it.

            ## Reading a frame correctly
            Reading is where real protocols go wrong, because a single `read` can return any number of bytes from 1 up to the buffer size. A correct reader handles three situations:

            1. **A clean end.** The stream ends exactly *between* frames: the first attempt to read a header returns 0. That's normal, not an error: the peer is finished. Return `Ok(None)`.
            2. **A truncated frame.** The stream ends *inside* a header or a payload. The peer crashed or the connection broke. That's an `UnexpectedEof` error.
            3. **Partial reads.** The header or payload arrives a few bytes at a time. Keep reading until you have all of it.

            `read_exact` handles (2) and (3) for a buffer of known size: it loops until the buffer is full and returns `UnexpectedEof` if the stream ends first. It can't handle (1) on its own, because it can't tell "ended before the first byte" from "ended after two bytes"; both are `UnexpectedEof`. So read the header with your own loop that notices whether *any* bytes arrived:

            ```rust
            let mut header = [0u8; 4];
            let mut filled = 0;
            while filled < header.len() {
                match reader.read(&mut header[filled..])? {
                    0 if filled == 0 => return Ok(None),          // clean end between frames
                    0 => return Err(io::ErrorKind::UnexpectedEof.into()),
                    read => filled += read,
                }
            }
            ```

            Then the payload, whose size is now known, with `read_exact`.

            A production loop also retries when `read` fails with `ErrorKind::Interrupted`, which happens when a signal arrives during the call; `read_exact` already does this internally.

            ## Never trust the length
            The length prefix comes from the other side of the network, so it's untrusted input. A malicious or buggy client can send `0xFF 0xFF 0xFF 0xFF`, claiming a 4 GB frame. If your reader allocates `vec![0; length]` first, one small message makes your server try to reserve 4 GB, and a handful of them can take it down: a **denial-of-service** attack that costs the attacker almost nothing.

            So: **check the length against a maximum before allocating.** Pick a limit that fits the protocol, such as a few megabytes, return `InvalidData` for anything bigger, and close the connection. Every serious protocol has a limit like this: HTTP servers cap header and body sizes for the same reason.

            ## Testing what the network will do to you
            On loopback, small writes usually arrive in one piece, so a reader that assumes "one read, one frame" passes casual tests. The reliable way to test is to make the worst case happen on purpose: wrap the input in a reader that returns **one byte per call**. Because the framing functions are generic over `Read`, a test can do exactly that without any network at all, which is how this lesson's grader works.

            ## Common mistakes
            - One `read` for the header and one for the payload.
            - Treating a clean end of stream as an error, or a truncated frame as a clean end.
            - Allocating from an untrusted length before checking it.
            - Writing the length with `as u32` or in native byte order.

            ## Try it locally
            1. Write two frames into a `Vec<u8>`, print it with `{:?}`, and find the length headers by eye.
            2. Implement the one-byte-at-a-time reader and run your `read_frame` against it, then against a version that reads the header with a single `read` call.
            3. Send a frame over a real `TcpStream` between two threads, writing the header and payload with a short sleep between them.
            """,
        Example = """
            use std::io::{self, Read};

            fn read_frame<R: Read>(reader: &mut R, max_len: usize) -> io::Result<Option<Vec<u8>>> {
                let mut header = [0u8; 4];
                // ... loop until 4 header bytes arrive; Ok(None) if the stream ends first ...
                let length = u32::from_be_bytes(header) as usize;
                if length > max_len {
                    return Err(io::Error::new(io::ErrorKind::InvalidData, "frame too large"));
                }
                let mut payload = vec![0u8; length]; // allocate only after the check
                reader.read_exact(&mut payload)?;
                Ok(Some(payload))
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Write and read length-prefixed frames",
            "Implement both halves of a length-prefixed protocol. The grader reads your frames through a reader that returns one byte per call, and checks every kind of end and every kind of bad length. Don't write a `main` function.",
            [
                "`fn write_frame<W: Write>(writer: &mut W, payload: &[u8]) -> io::Result<()>` writes a 4-byte big-endian length, then the payload; a payload too large for a `u32` length is `InvalidInput`",
                "`fn read_frame<R: Read>(reader: &mut R, max_len: usize) -> io::Result<Option<Vec<u8>>>` returns `Some(payload)` for each frame (an empty frame is `Some` of an empty `Vec`) and `None` when the stream ends cleanly between frames; it must work when each `read` returns a single byte",
                "A length above `max_len` is `InvalidData`, checked before allocating; a stream that ends inside a header or a payload is `UnexpectedEof`",
            ],
            """
            use std::io::{self, Read, Write};

            fn write_frame<W: Write>(writer: &mut W, payload: &[u8]) -> io::Result<()> {
                todo!()
            }

            fn read_frame<R: Read>(reader: &mut R, max_len: usize) -> io::Result<Option<Vec<u8>>> {
                todo!()
            }
            """,
            null,
            [],
            "Write with `u32::try_from(payload.len())`, `to_be_bytes()` and `write_all`. Read the header with a loop over `reader.read(&mut header[filled..])`, returning `Ok(None)` if the very first read returns 0 and `UnexpectedEof` if a later one does. Check `length > max_len` before `vec![0u8; length]`, then `read_exact` the payload.",
            ["Frames are written in network byte order", "Frames are reassembled from any read sizes", "Oversized and truncated frames are rejected"]),
    };

    private static Lesson RustNetConcurrencyTimeouts(Lesson lesson) => lesson with
    {
        Subtitle = "Serve many clients at once with a thread per connection, give up on clients that go silent, and understand the limits that lead to async I/O.",
        Concept = "A server that handles one connection at a time is stalled by its slowest client. Handling each connection on its own thread fixes that, as long as every connection has a read timeout so a silent client can't hold its thread forever, and the number of connections is bounded. When you need tens of thousands of mostly idle connections, threads become too expensive, which is the problem async I/O solves.",
        Body = """
            ## One client at a time
            The echo server in the first lesson accepts a connection, serves it until it ends, then accepts the next. That works until a client is slow. Suppose a client connects and then waits: a person typing into `nc`, a phone on a bad mobile connection, or a program that hung. Every other client queues behind it, unable to get a single byte of service. On the public internet this isn't hypothetical: an attack called **Slowloris** opens many connections and sends bytes very slowly precisely to occupy a server.

            ## A thread per connection
            The straightforward fix uses the concurrency lesson's tools: accept on one thread, and hand each connection to a new thread.

            ```rust
            use std::net::TcpListener;
            use std::thread;

            let listener = TcpListener::bind("127.0.0.1:7878")?;
            for stream in listener.incoming() {
                let stream = stream?;
                thread::spawn(move || {
                    if let Err(error) = handle_client(stream) {
                        eprintln!("client error: {error}");
                    }
                });
            }
            ```

            `incoming()` is an iterator that calls `accept` each time. The `move` closure takes ownership of the `TcpStream`, which is `Send`, so it can go to another thread. Each client now waits only on itself.

            A thread costs memory, by default a 2 MB stack reservation per spawned Rust thread, plus kernel bookkeeping and the cost of switching between threads. Hundreds or a few thousand threads are fine on a modern machine. Tens of thousands of mostly idle ones are not: that is the **C10K problem**, and it's why the next lesson uses async I/O.

            Whatever the design, **bound it**. An unbounded `thread::spawn` per connection means an attacker who opens 100,000 connections makes you create 100,000 threads. Real servers cap concurrent connections (a counter or semaphore checked before spawning) or use a fixed-size **thread pool** that queues work, and reject or close connections beyond the limit.

            ## Timeouts
            Concurrency stops a slow client from blocking *others*. It doesn't stop that client from occupying its own thread, and its socket, forever. Every blocking network operation needs a time limit:

            - `stream.set_read_timeout(Some(duration))`: a `read` that waits longer than this fails instead of blocking.
            - `stream.set_write_timeout(Some(duration))`: the same for writes, which can block when the peer stops reading.
            - `TcpStream::connect_timeout(&address, duration)`: don't wait minutes for an unreachable host.

            A timed-out read returns an error whose kind depends on the platform: **`WouldBlock`** on Linux and macOS, **`TimedOut`** on Windows. Portable code checks for both. Code that reports errors upward should translate them into one meaning, typically `TimedOut`, so callers don't need to know the platform.

            Read the timeout as an **idle** limit: it applies to each `read` call, so a client that sends a little data every few seconds keeps its connection alive. That's usually what you want for interactive protocols. When you need a limit on the *whole* request ("finish within 30 seconds no matter what"), track a deadline yourself and reduce the timeout before each read.

            ## A line-based protocol
            Line-delimited text is the simplest framing for a request/response protocol, and easy to test by hand with `nc`. With `BufReader` on one handle to the socket and writes on a clone:

            ```rust
            let mut writer = stream.try_clone()?;   // a second handle to the same socket
            let mut reader = BufReader::new(stream);
            let mut line = String::new();
            loop {
                line.clear();
                if reader.read_line(&mut line)? == 0 {
                    break;                          // client closed the connection
                }
                writeln!(writer, "you said: {}", line.trim())?;
            }
            ```

            `try_clone` duplicates the file descriptor, so reading and writing can be owned separately. Each command gets exactly one reply line, which makes the protocol easy for clients to follow. Invalid input gets an error *reply*, not a dropped connection, so a client can recover.

            ## Shutting down cleanly
            A server stops accepting new connections, lets in-progress ones finish (each bounded by its timeouts), then exits. With threads, keep the `JoinHandle`s and `join` them, or use `thread::scope`, which waits for every thread spawned inside it before returning. Joining also surfaces a handler that panicked: `join()` returns `Err`, which you should log rather than ignore.

            ## Toward async
            Under the hood, a thread blocked in `read` is just waiting for the kernel to say "data arrived". Operating systems offer a better way to wait: **readiness notification** (`epoll` on Linux, `kqueue` on macOS and BSD, IOCP on Windows) lets one thread ask about thousands of sockets at once and handle whichever are ready. Programming against that directly is intricate. Rust's async model, with a runtime like Tokio, gives you code that reads like the blocking version but runs thousands of connections on a few threads. That's the next lesson.

            ## Common mistakes
            - No read timeout, so silent clients accumulate until the server runs out of threads or file descriptors.
            - Spawning without any limit.
            - Checking only `TimedOut` (or only `WouldBlock`) and missing the timeout on the other platform.
            - Closing the connection on every malformed command instead of replying with an error.
            - Ignoring the result of `join`, hiding panics in handlers.

            ## Try it locally
            1. Run a sequential version of the server, connect two `nc` sessions, and confirm the second is stuck. Switch to a thread per connection.
            2. Add a 10-second read timeout, connect with `nc` and wait. Print the error kind you get.
            3. Write a small client that opens 2,000 connections and holds them; watch the server's thread count in `top` or Task Manager.
            """,
        Example = """
            use std::net::{TcpListener, TcpStream};
            use std::time::Duration;

            fn serve_forever(listener: TcpListener) -> std::io::Result<()> {
                for stream in listener.incoming() {
                    let stream: TcpStream = stream?;
                    stream.set_read_timeout(Some(Duration::from_secs(30)))?;
                    std::thread::spawn(move || handle(stream));
                }
                Ok(())
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Serve concurrent clients with idle timeouts",
            "Implement a small line protocol and a server that handles a fixed number of clients concurrently. The grader keeps one client silent while another must be served within 400 ms, and expects the silent one to time out. Don't write a `main` function.",
            [
                "`fn handle_client(stream: TcpStream, idle: Duration) -> io::Result<i64>`: for each line `ADD <n>` add n to a running total and reply `TOTAL <total>`; for `QUIT` reply `BYE <total>` and return the total; for anything else reply `ERR bad command`; when the client closes, return the total",
                "If no data arrives for `idle`, return an error of kind `TimedOut` (on every platform)",
                "`fn serve(listener: &TcpListener, clients: usize, idle: Duration) -> Vec<io::Result<i64>>` accepts exactly `clients` connections, handles each on its own thread, and returns their results in the order the connections were accepted",
            ],
            """
            use std::io::{self, BufRead, BufReader, Write};
            use std::net::{TcpListener, TcpStream};
            use std::thread;
            use std::time::Duration;

            fn handle_client(stream: TcpStream, idle: Duration) -> io::Result<i64> {
                todo!()
            }

            fn serve(listener: &TcpListener, clients: usize, idle: Duration) -> Vec<io::Result<i64>> {
                todo!()
            }
            """,
            null,
            [],
            "Call `set_read_timeout(Some(idle))` first, keep a `try_clone()` for writing, and read lines from a `BufReader`. When `read_line` fails with `WouldBlock` or `TimedOut`, return `io::Error::new(io::ErrorKind::TimedOut, ...)`. In `serve`, accept and `thread::spawn` all the clients first, keeping the `JoinHandle`s in order, then join them.",
            ["Commands get the right replies", "Silent clients time out", "Clients are served concurrently", "Results come back in accept order"]),
    };

    private static Lesson RustNetTokioKv(Lesson lesson) => lesson with
    {
        Subtitle = "Rebuild the server on Tokio: a task per connection, shared state behind a mutex, and a small key-value protocol, as the bridge into HTTP services.",
        Concept = "With Tokio, `TcpListener::accept().await` and `read().await` look like blocking code, but waiting suspends only the current task, not the thread. Spawning one lightweight task per connection lets a few threads serve thousands of clients. State shared between connections lives in an `Arc<Mutex<...>>`, locked only for the moment it's touched and never held across an `.await`.",
        Body = """
            ## Same shape, different engine
            The async lesson introduced futures and the Tokio runtime for waiting on timers and slow calls. Network servers are where async earns its keep. Here is the thread-per-connection server translated to Tokio:

            ```rust
            use tokio::net::TcpListener;

            #[tokio::main]
            async fn main() -> std::io::Result<()> {
                let listener = TcpListener::bind("127.0.0.1:7878").await?;
                loop {
                    let (stream, _) = listener.accept().await?;
                    tokio::spawn(async move {
                        if let Err(error) = handle(stream).await {
                            eprintln!("client error: {error}");
                        }
                    });
                }
            }
            ```

            It reads almost exactly like the threaded version. The difference is underneath. `tokio::spawn` creates a **task**, a future the runtime schedules, which costs a few hundred bytes rather than a thread's megabytes. When a task awaits a socket with no data, it's set aside, and the runtime uses the operating system's readiness notifications (`epoll`, `kqueue`) to wake it when data arrives. A handful of threads can therefore serve tens of thousands of mostly idle connections.

            The rule from the async lesson matters even more here: **never block inside a task.** A `std::thread::sleep`, a blocking file read, or a heavy computation in a task freezes every other task on that thread. Use Tokio's async versions, or move blocking work to `tokio::task::spawn_blocking`.

            ## Tokio's networking types
            `tokio::net::TcpListener` and `TcpStream` mirror the standard library's, with `async` methods. Reading and writing come from extension traits in `tokio::io`:

            - `AsyncReadExt`: `read`, `read_exact`, `read_to_end`, `read_u8`, `read_u32` (big-endian), ...
            - `AsyncWriteExt`: `write_all`, `flush`, `shutdown`.
            - `AsyncBufReadExt` on `tokio::io::BufReader`: `read_line` and `lines()`, whose `next_line().await` returns `Ok(Some(line))`, or `Ok(None)` when the client closes.

            To read and write a connection independently, split it: `let (reader, mut writer) = stream.into_split();` gives two owned halves.

            ```rust
            use tokio::io::{AsyncBufReadExt, AsyncWriteExt, BufReader};

            async fn handle(stream: tokio::net::TcpStream) -> std::io::Result<()> {
                let (reader, mut writer) = stream.into_split();
                let mut lines = BufReader::new(reader).lines();
                while let Some(line) = lines.next_line().await? {
                    writer.write_all(format!("you said: {line}\n").as_bytes()).await?;
                }
                Ok(())
            }
            ```

            Timeouts work by wrapping any future: `tokio::time::timeout(Duration::from_secs(30), lines.next_line()).await` returns `Err(Elapsed)` if the read takes too long.

            ## Sharing state between connections
            A key-value server keeps one map that every connection reads and writes. Tasks may run on different threads, so the map must be shared safely, using the same tools as the smart-pointers and concurrency lessons:

            ```rust
            use std::collections::HashMap;
            use std::sync::{Arc, Mutex};

            type Store = Arc<Mutex<HashMap<String, String>>>;
            ```

            `Arc` gives each task its own owning handle to the same map; `Mutex` lets one task at a time change it. Clone the `Arc` (cheap: it increments a counter) before moving it into each spawned task.

            Which `Mutex`? Tokio provides an async `tokio::sync::Mutex`, but the standard `std::sync::Mutex` is the right choice when the lock is held only briefly and **never across an `.await`**. Holding a std `MutexGuard` across an `.await` is a bug: the task can be suspended while holding the lock, and the compiler usually rejects it anyway because the guard isn't `Send`. The clean pattern is to keep all the locking inside an ordinary, non-async function:

            ```rust
            fn apply(command: &str, store: &Store) -> String {
                let mut map = store.lock().unwrap();   // locked here...
                // parse the command, read or change the map, build the reply
                String::from("OK")
            }                                          // ...and unlocked here, before any await
            ```

            A pure `apply` function has another benefit: you can test the whole protocol without opening a socket.

            ## The protocol
            One command per line, one reply line per command:

            - `SET <key> <value>` stores the value (which may contain spaces) and replies `OK`.
            - `GET <key>` replies `VALUE <value>` or `NOT_FOUND`.
            - `DEL <key>` replies `DELETED` or `NOT_FOUND`.
            - `COUNT` replies `COUNT <number of keys>`.
            - Anything else replies `ERR unknown command`.

            Splitting with `splitn(3, ' ')` gives at most three pieces, so everything after the key stays together as the value. Matching on the tuple of pieces, `(Some("SET"), Some(key), Some(value))`, handles both the command and the wrong-number-of-arguments cases in one `match`.

            Redis, a widely used production database, began with a protocol not far from this one, and it is still line-oriented.

            ## Milestones beyond the grader
            The graded exercise is the core. To make it a project you can present, extend it on your own machine:

            1. **A real binary** that binds to `127.0.0.1:6380`, and manual testing with `nc`.
            2. **Persistence**: append every `SET` and `DEL` to a log file using the files lesson's techniques, and replay it at startup. Then decide when to call `sync_all`, and measure what each choice costs.
            3. **Limits and timeouts**: a maximum line length (a client sending a 1 GB line is the framing lesson's attack again), an idle timeout per connection, and a cap on concurrent connections using `tokio::sync::Semaphore`.
            4. **Graceful shutdown**: use `tokio::signal::ctrl_c()` and `tokio::select!` to stop accepting, let connections finish, and flush the log.
            5. **Measure**: write a load generator that opens 1,000 connections and sends commands; record throughput and latency, then try a sharded store (several mutexes, picked by key hash) and compare.
            6. **Explain** your choice of `std::sync::Mutex`, how the server behaves under a slow client, and what your measurements showed.

            The service-delivery lessons that follow build HTTP APIs with Axum, which runs on exactly this foundation: Tokio, a listener, a task per connection, and shared state passed to handlers.

            ## Common mistakes
            - Awaiting each connection inside the accept loop instead of spawning it, so one idle client stops the server.
            - Blocking calls inside tasks.
            - Holding a `MutexGuard` across `.await`.
            - Splitting the whole line on every space, so values can't contain spaces.
            """,
        Example = """
            use std::sync::Arc;
            use tokio::net::TcpListener;

            async fn serve(listener: TcpListener, store: Store) {
                loop {
                    let Ok((stream, _)) = listener.accept().await else { continue };
                    let store = Arc::clone(&store);
                    tokio::spawn(async move {
                        let _ = handle(stream, store).await; // one task per connection
                    });
                }
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Build an async key-value server",
            "Implement the protocol as a pure function, a per-connection handler, and an accept loop. The grader tests `apply` directly, then connects several clients over 127.0.0.1, one of which stays silent the whole time. Tokio (with networking) is available. Don't write a `main` function.",
            [
                "`fn apply(command: &str, store: &Store) -> String` implements `SET <key> <value>` → `OK`, `GET <key>` → `VALUE <value>` or `NOT_FOUND`, `DEL <key>` → `DELETED` or `NOT_FOUND`, `COUNT` → `COUNT <n>`, and anything else (including missing arguments) → `ERR unknown command`; values may contain spaces",
                "`async fn handle(stream: TcpStream, store: Store) -> std::io::Result<()>` replies to each line with `apply`'s result plus a newline until the client closes",
                "`async fn serve(listener: TcpListener, store: Store)` accepts connections forever, handling each in its own spawned task so one idle client never delays others",
            ],
            """
            use std::collections::HashMap;
            use std::sync::{Arc, Mutex};
            use tokio::io::{AsyncBufReadExt, AsyncWriteExt, BufReader};
            use tokio::net::{TcpListener, TcpStream};

            type Store = Arc<Mutex<HashMap<String, String>>>;

            fn apply(command: &str, store: &Store) -> String {
                todo!()
            }

            async fn handle(stream: TcpStream, store: Store) -> std::io::Result<()> {
                todo!()
            }

            async fn serve(listener: TcpListener, store: Store) {
                todo!()
            }
            """,
            null,
            [],
            "In `apply`, `let mut parts = command.trim().splitn(3, ' ');` then `match (parts.next(), parts.next(), parts.next())`, locking the store once at the top. In `handle`, `stream.into_split()`, then `BufReader::new(reader).lines()` and `while let Some(line) = lines.next_line().await?`. In `serve`, clone the `Arc` and `tokio::spawn` a task for every accepted stream.",
            ["The protocol replies are exact", "Every connection shares one store", "An idle connection doesn't block others"]),
    };
}
