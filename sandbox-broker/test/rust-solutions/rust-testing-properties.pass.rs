#[derive(Debug, PartialEq)]
enum DecodeError {
    OddLength,
    ZeroCount,
}

fn encode(data: &[u8]) -> Vec<u8> {
    let mut encoded = Vec::new();
    let mut iter = data.iter().peekable();
    while let Some(&byte) = iter.next() {
        let mut count: u8 = 1;
        while count < u8::MAX && iter.peek() == Some(&&byte) {
            iter.next();
            count += 1;
        }
        encoded.push(count);
        encoded.push(byte);
    }
    encoded
}

fn decode(encoded: &[u8]) -> Result<Vec<u8>, DecodeError> {
    if encoded.len() % 2 != 0 {
        return Err(DecodeError::OddLength);
    }
    let mut data = Vec::new();
    for pair in encoded.chunks_exact(2) {
        let (count, byte) = (pair[0], pair[1]);
        if count == 0 {
            return Err(DecodeError::ZeroCount);
        }
        data.extend(std::iter::repeat_n(byte, count as usize));
    }
    Ok(data)
}
