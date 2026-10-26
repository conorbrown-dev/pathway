/// Reads the `index`th `u32` after `ptr`.
///
/// # Safety
/// `ptr` must point to at least `index + 1` initialized, properly aligned `u32`s that stay valid
/// and unmodified for the duration of the call.
unsafe fn read_nth(ptr: *const u32, index: usize) -> u32 {
    // SAFETY: the caller guarantees `ptr.add(index)` is in bounds of one live allocation.
    unsafe { *ptr.add(index) }
}

fn checked_read(values: &[u32], index: usize) -> Option<u32> {
    if index >= values.len() {
        return None;
    }
    // SAFETY: index < values.len(), and a slice's elements are initialized, aligned and borrowed
    // for the whole call.
    Some(unsafe { read_nth(values.as_ptr(), index) })
}

fn element_stride(values: &[u64]) -> usize {
    if values.len() < 2 {
        return 0;
    }
    let first = &values[0] as *const u64 as usize;
    let second = &values[1] as *const u64 as usize;
    second - first
}
