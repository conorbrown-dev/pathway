use std::io::{self, Read, Write};
use std::net::{Shutdown, SocketAddr, TcpListener, TcpStream};

fn local_listener() -> io::Result<(TcpListener, SocketAddr)> {
    let listener = TcpListener::bind("127.0.0.1:0")?;
    let address = listener.local_addr()?;
    Ok((listener, address))
}

fn echo_once(listener: &TcpListener) -> io::Result<usize> {
    let (mut stream, _peer) = listener.accept()?;
    let mut buffer = [0u8; 4096];
    let mut total = 0;
    loop {
        let read = stream.read(&mut buffer)?;
        if read == 0 {
            return Ok(total);
        }
        stream.write_all(&buffer[..read])?;
        total += read;
    }
}

fn request(address: SocketAddr, message: &[u8]) -> io::Result<Vec<u8>> {
    let mut stream = TcpStream::connect(address)?;
    stream.write_all(message)?;
    stream.shutdown(Shutdown::Write)?;
    let mut reply = Vec::new();
    stream.read_to_end(&mut reply)?;
    Ok(reply)
}
