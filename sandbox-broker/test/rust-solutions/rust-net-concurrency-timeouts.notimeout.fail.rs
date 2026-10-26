use std::io::{self, BufRead, BufReader, Write};
use std::net::{TcpListener, TcpStream};
use std::thread;
use std::time::Duration;

// Never sets a read timeout, so a silent client keeps its thread forever.
fn handle_client(stream: TcpStream, _idle: Duration) -> io::Result<i64> {
    let mut writer = stream.try_clone()?;
    let reader = BufReader::new(stream);
    let mut total = 0i64;
    for line in reader.lines() {
        let line = line?;
        if line.trim() == "QUIT" {
            writeln!(writer, "BYE {total}")?;
            return Ok(total);
        }
        match line.trim().strip_prefix("ADD ").and_then(|n| n.parse::<i64>().ok()) {
            Some(amount) => {
                total += amount;
                writeln!(writer, "TOTAL {total}")?;
            }
            None => writeln!(writer, "ERR bad command")?,
        }
    }
    Ok(total)
}

fn serve(listener: &TcpListener, clients: usize, idle: Duration) -> Vec<io::Result<i64>> {
    let handles: Vec<_> = (0..clients)
        .map(|_| {
            let (stream, _) = listener.accept().unwrap();
            thread::spawn(move || handle_client(stream, idle))
        })
        .collect();
    handles.into_iter().map(|handle| handle.join().unwrap()).collect()
}
