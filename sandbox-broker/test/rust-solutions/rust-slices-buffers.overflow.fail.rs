fn split_header(packet: &[u8], header_len: usize) -> Option<(&[u8], &[u8])> {
    if header_len > packet.len() {
        return None;
    }
    Some(packet.split_at(header_len))
}

// offset + len can overflow and wrap around (or panic in a debug build).
fn read_at(buffer: &[u8], offset: usize, len: usize) -> Option<&[u8]> {
    if offset + len > buffer.len() {
        return None;
    }
    Some(&buffer[offset..offset + len])
}

fn find_subslice(haystack: &[u8], needle: &[u8]) -> Option<usize> {
    if needle.is_empty() {
        return Some(0);
    }
    haystack.windows(needle.len()).position(|window| window == needle)
}

fn chunk_sums(data: &[u8], size: usize) -> Vec<u32> {
    if size == 0 {
        return Vec::new();
    }
    data.chunks(size).map(|chunk| chunk.iter().map(|&b| u32::from(b)).sum()).collect()
}
