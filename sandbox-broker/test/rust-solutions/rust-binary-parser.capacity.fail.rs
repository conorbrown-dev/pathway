#[derive(Debug, PartialEq)]
struct Record {
    id: u32,
    name: String,
}

#[derive(Debug, PartialEq)]
enum ParseError {
    BadMagic,
    UnsupportedVersion(u8),
    Truncated,
    InvalidUtf8,
    TrailingBytes,
}

// Indexes directly, so a short input panics instead of returning Truncated.
fn parse(bytes: &[u8]) -> Result<Vec<Record>, ParseError> {
    if bytes.len() < 4 || &bytes[..4] != b"PWAY" {
        return Err(ParseError::BadMagic);
    }
    if bytes[4] != 1 {
        return Err(ParseError::UnsupportedVersion(bytes[4]));
    }
    let count = u16::from_be_bytes([bytes[5], bytes[6]]);
    let mut offset = 7;
    let mut records = Vec::new();
    for _ in 0..count {
        let id = u32::from_be_bytes(bytes[offset..offset + 4].try_into().unwrap());
        let length = bytes[offset + 4] as usize;
        let name = std::str::from_utf8(&bytes[offset + 5..offset + 5 + length]).map_err(|_| ParseError::InvalidUtf8)?;
        records.push(Record { id, name: name.to_string() });
        offset += 5 + length;
    }
    if offset != bytes.len() {
        return Err(ParseError::TrailingBytes);
    }
    Ok(records)
}
