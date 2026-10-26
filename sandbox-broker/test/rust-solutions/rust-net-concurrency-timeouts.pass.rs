use std::io::{self, BufRead, BufReader, Write};
use std::net::{TcpListener, TcpStream};
use std::thread;
use std::time::Duration;

fn handle_client(stream: TcpStream, idle: Duration) -> io::Result<i64> {
    stream.set_read_timeout(Some(idle))?;
    let mut writer = stream.try_clone()?;
    let mut reader = BufReader::new(stream);
    let mut total: i64 = 0;
    let mut line = String::new();
    loop {
        line.clear();
        let read = match reader.read_line(&mut line) {
            Ok(read) => read,
            Err(error) if matches!(error.kind(), io::ErrorKind::WouldBlock | io::ErrorKind::TimedOut) => {
                return Err(io::Error::new(io::ErrorKind::TimedOut, "client was idle too long"));
            }
            Err(error) => return Err(error),
        };
        if read == 0 {
            return Ok(total);
        }
        let command = line.trim();
        if command == "QUIT" {
            writeln!(writer, "BYE {total}")?;
            return Ok(total);
        }
        match command.strip_prefix("ADD ").and_then(|number| number.trim().parse::<i64>().ok()) {
            Some(amount) => {
                total += amount;
                writeln!(writer, "TOTAL {total}")?;
            }
            None => writeln!(writer, "ERR bad command")?,
        }
    }
}

fn serve(listener: &TcpListener, clients: usize, idle: Duration) -> Vec<io::Result<i64>> {
    let mut handles = Vec::new();
    for _ in 0..clients {
        match listener.accept() {
            Ok((stream, _)) => handles.push(Some(thread::spawn(move || handle_client(stream, idle)))),
            Err(error) => {
                handles.push(None);
                eprintln!("accept failed: {error}");
            }
        }
    }
    handles
        .into_iter()
        .map(|handle| match handle {
            Some(handle) => handle.join().unwrap_or_else(|_| Err(io::Error::other("handler panicked"))),
            None => Err(io::Error::other("accept failed")),
        })
        .collect()
}
