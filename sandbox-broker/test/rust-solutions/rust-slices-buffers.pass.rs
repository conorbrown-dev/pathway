fn split_header(packet: &[u8], header_len: usize) -> Option<(&[u8], &[u8])> {
    packet.split_at_checked(header_len)
}

fn read_at(buffer: &[u8], offset: usize, len: usize) -> Option<&[u8]> {
    let end = offset.checked_add(len)?;
    buffer.get(offset..end)
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
