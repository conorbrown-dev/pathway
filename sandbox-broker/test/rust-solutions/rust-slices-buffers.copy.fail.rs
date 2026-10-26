// Copies the bytes into new Vecs and leaks them to get slices back: not zero-copy.
fn split_header(packet: &[u8], header_len: usize) -> Option<(&[u8], &[u8])> {
    if header_len > packet.len() {
        return None;
    }
    let header: &'static [u8] = Box::leak(packet[..header_len].to_vec().into_boxed_slice());
    let body: &'static [u8] = Box::leak(packet[header_len..].to_vec().into_boxed_slice());
    Some((header, body))
}

fn read_at(buffer: &[u8], offset: usize, len: usize) -> Option<&[u8]> {
    buffer.get(offset..offset.checked_add(len)?)
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
