use std::io::{self, Read, Write};

fn write_frame<W: Write>(writer: &mut W, payload: &[u8]) -> io::Result<()> {
    writer.write_all(&(payload.len() as u32).to_be_bytes())?;
    writer.write_all(payload)
}

// Assumes one read() returns the whole header and the whole payload.
fn read_frame<R: Read>(reader: &mut R, max_len: usize) -> io::Result<Option<Vec<u8>>> {
    let mut header = [0u8; 4];
    if reader.read(&mut header)? == 0 {
        return Ok(None);
    }
    let length = u32::from_be_bytes(header) as usize;
    if length > max_len {
        return Err(io::Error::new(io::ErrorKind::InvalidData, "too large"));
    }
    let mut payload = vec![0u8; length];
    let read = reader.read(&mut payload)?;
    payload.truncate(read);
    Ok(Some(payload))
}
