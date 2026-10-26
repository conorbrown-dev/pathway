use std::io::{self, BufRead, BufReader, Write};
use std::net::{TcpListener, TcpStream};
use std::time::Duration;

fn handle_client(mut stream: TcpStream, idle: Duration) -> io::Result<i64> {
    stream.set_read_timeout(Some(idle))?;
    let reader = BufReader::new(stream.try_clone()?);
    let mut total = 0i64;
    for line in reader.lines() {
        let line = line.map_err(|error| match error.kind() {
            io::ErrorKind::WouldBlock | io::ErrorKind::TimedOut => io::Error::new(io::ErrorKind::TimedOut, error),
            _ => error,
        })?;
        let mut words = line.split_whitespace();
        match (words.next(), words.next().map(str::parse::<i64>), words.next()) {
            (Some("QUIT"), None, None) => {
                stream.write_all(format!("BYE {total}\n").as_bytes())?;
                return Ok(total);
            }
            (Some("ADD"), Some(Ok(amount)), None) => {
                total += amount;
                stream.write_all(format!("TOTAL {total}\n").as_bytes())?;
            }
            _ => stream.write_all(b"ERR bad command\n")?,
        }
    }
    Ok(total)
}

fn serve(listener: &TcpListener, clients: usize, idle: Duration) -> Vec<io::Result<i64>> {
    std::thread::scope(|scope| {
        let handles: Vec<_> = (0..clients)
            .map(|_| {
                let accepted = listener.accept();
                scope.spawn(move || handle_client(accepted?.0, idle))
            })
            .collect();
        handles.into_iter().map(|handle| handle.join().expect("handler panicked")).collect()
    })
}
