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

struct Cursor<'a> {
    rest: &'a [u8],
}

impl<'a> Cursor<'a> {
    fn take(&mut self, count: usize) -> Result<&'a [u8], ParseError> {
        let (taken, rest) = self.rest.split_at_checked(count).ok_or(ParseError::Truncated)?;
        self.rest = rest;
        Ok(taken)
    }

    fn u8(&mut self) -> Result<u8, ParseError> {
        Ok(self.take(1)?[0])
    }

    fn u16_be(&mut self) -> Result<u16, ParseError> {
        Ok(u16::from_be_bytes(self.take(2)?.try_into().unwrap()))
    }

    fn u32_be(&mut self) -> Result<u32, ParseError> {
        Ok(u32::from_be_bytes(self.take(4)?.try_into().unwrap()))
    }
}

fn parse(bytes: &[u8]) -> Result<Vec<Record>, ParseError> {
    let mut cursor = Cursor { rest: bytes };
    if cursor.take(4).map_err(|_| ParseError::BadMagic)? != b"PWAY" {
        return Err(ParseError::BadMagic);
    }
    let version = cursor.u8()?;
    if version != 1 {
        return Err(ParseError::UnsupportedVersion(version));
    }
    let count = cursor.u16_be()?;
    let mut records = Vec::new();
    for _ in 0..count {
        let id = cursor.u32_be()?;
        let length = cursor.u8()? as usize;
        let name = std::str::from_utf8(cursor.take(length)?).map_err(|_| ParseError::InvalidUtf8)?;
        records.push(Record { id, name: name.to_string() });
    }
    if !cursor.rest.is_empty() {
        return Err(ParseError::TrailingBytes);
    }
    Ok(records)
}
