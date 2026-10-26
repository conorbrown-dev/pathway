#[derive(Debug, PartialEq)]
enum DecodeError {
    OddLength,
    ZeroCount,
}

fn encode(data: &[u8]) -> Vec<u8> {
    let mut encoded = Vec::new();
    let mut i = 0;
    while i < data.len() {
        let mut run = 1;
        while run < 255 && i + run < data.len() && data[i + run] == data[i] {
            run += 1;
        }
        encoded.push(run as u8);
        encoded.push(data[i]);
        i += run;
    }
    encoded
}

// Indexes pairs directly: an odd-length input panics instead of returning OddLength.
fn decode(encoded: &[u8]) -> Result<Vec<u8>, DecodeError> {
    let mut data = Vec::new();
    let mut i = 0;
    while i < encoded.len() {
        let (count, byte) = (encoded[i], encoded[i + 1]);
        if count == 0 {
            return Err(DecodeError::ZeroCount);
        }
        data.extend(std::iter::repeat_n(byte, count as usize));
        i += 2;
    }
    Ok(data)
}
