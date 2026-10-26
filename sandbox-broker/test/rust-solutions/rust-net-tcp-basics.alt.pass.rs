use std::io::{self, Read, Write};
use std::net::{Ipv4Addr, Shutdown, SocketAddr, TcpListener, TcpStream};

fn local_listener() -> io::Result<(TcpListener, SocketAddr)> {
    let listener = TcpListener::bind((Ipv4Addr::LOCALHOST, 0))?;
    Ok((listener.try_clone()?, listener.local_addr()?))
}

fn echo_once(listener: &TcpListener) -> io::Result<usize> {
    let (stream, _) = listener.accept()?;
    let mut writer = stream.try_clone()?;
    let copied = io::copy(&mut &stream, &mut writer)?;
    Ok(copied as usize)
}

fn request(address: SocketAddr, message: &[u8]) -> io::Result<Vec<u8>> {
    let mut stream = TcpStream::connect(address)?;
    stream.write_all(message)?;
    stream.shutdown(Shutdown::Write)?;
    let mut reply = Vec::new();
    stream.read_to_end(&mut reply)?;
    Ok(reply)
}
