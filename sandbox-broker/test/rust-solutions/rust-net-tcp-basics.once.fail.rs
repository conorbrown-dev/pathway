use std::io::{self, Read, Write};
use std::net::{Shutdown, SocketAddr, TcpListener, TcpStream};

fn local_listener() -> io::Result<(TcpListener, SocketAddr)> {
    let listener = TcpListener::bind("127.0.0.1:0")?;
    let address = listener.local_addr()?;
    Ok((listener, address))
}

// One read is not one message: TCP delivers a byte stream in whatever pieces it likes.
fn echo_once(listener: &TcpListener) -> io::Result<usize> {
    let (mut stream, _) = listener.accept()?;
    let mut buffer = [0u8; 4096];
    let read = stream.read(&mut buffer)?;
    stream.write_all(&buffer[..read])?;
    Ok(read)
}

fn request(address: SocketAddr, message: &[u8]) -> io::Result<Vec<u8>> {
    let mut stream = TcpStream::connect(address)?;
    stream.write_all(message)?;
    stream.shutdown(Shutdown::Write)?;
    let mut reply = Vec::new();
    stream.read_to_end(&mut reply)?;
    Ok(reply)
}
