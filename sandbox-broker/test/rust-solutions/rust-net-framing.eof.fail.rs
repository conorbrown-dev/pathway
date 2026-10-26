use std::io::{self, Read, Write};

fn write_frame<W: Write>(writer: &mut W, payload: &[u8]) -> io::Result<()> {
    writer.write_all(&(payload.len() as u32).to_be_bytes())?;
    writer.write_all(payload)
}

// read_exact on the header turns a clean end of stream into an error,
// and the size check happens only after allocating.
fn read_frame<R: Read>(reader: &mut R, max_len: usize) -> io::Result<Option<Vec<u8>>> {
    let mut header = [0u8; 4];
    reader.read_exact(&mut header)?;
    let mut payload = vec![0u8; u32::from_be_bytes(header) as usize];
    if payload.len() > max_len {
        return Err(io::Error::new(io::ErrorKind::InvalidData, "too large"));
    }
    reader.read_exact(&mut payload)?;
    Ok(Some(payload))
}
