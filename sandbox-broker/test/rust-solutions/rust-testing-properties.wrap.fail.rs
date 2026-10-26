#[derive(Debug, PartialEq)]
enum DecodeError {
    OddLength,
    ZeroCount,
}

// Counts runs in a u8 with wrapping_add: a run of 256 bytes is written as a count of 0.
fn encode(data: &[u8]) -> Vec<u8> {
    let mut encoded: Vec<u8> = Vec::new();
    for &byte in data {
        match encoded.len() {
            n if n >= 2 && encoded[n - 1] == byte => encoded[n - 2] = encoded[n - 2].wrapping_add(1),
            _ => {
                encoded.push(1);
                encoded.push(byte);
            }
        }
    }
    encoded
}

fn decode(encoded: &[u8]) -> Result<Vec<u8>, DecodeError> {
    if encoded.len() % 2 != 0 {
        return Err(DecodeError::OddLength);
    }
    let mut data = Vec::new();
    for pair in encoded.chunks_exact(2) {
        if pair[0] == 0 {
            return Err(DecodeError::ZeroCount);
        }
        data.extend(std::iter::repeat_n(pair[1], pair[0] as usize));
    }
    Ok(data)
}
