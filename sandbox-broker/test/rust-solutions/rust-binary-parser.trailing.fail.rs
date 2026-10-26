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

fn take<'a>(rest: &mut &'a [u8], count: usize) -> Result<&'a [u8], ParseError> {
    let (taken, remaining) = rest.split_at_checked(count).ok_or(ParseError::Truncated)?;
    *rest = remaining;
    Ok(taken)
}

// Ignores bytes after the last record and uses lossy UTF-8.
fn parse(bytes: &[u8]) -> Result<Vec<Record>, ParseError> {
    let mut rest = bytes;
    if take(&mut rest, 4).map_err(|_| ParseError::BadMagic)? != b"PWAY" {
        return Err(ParseError::BadMagic);
    }
    let version = take(&mut rest, 1)?[0];
    if version != 1 {
        return Err(ParseError::UnsupportedVersion(version));
    }
    let count = u16::from_be_bytes(take(&mut rest, 2)?.try_into().unwrap());
    let mut records = Vec::new();
    for _ in 0..count {
        let id = u32::from_be_bytes(take(&mut rest, 4)?.try_into().unwrap());
        let length = take(&mut rest, 1)?[0] as usize;
        let name = String::from_utf8_lossy(take(&mut rest, length)?).into_owned();
        records.push(Record { id, name });
    }
    Ok(records)
}
