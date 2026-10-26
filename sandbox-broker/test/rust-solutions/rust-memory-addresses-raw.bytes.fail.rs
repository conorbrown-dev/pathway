/// # Safety
/// `ptr` must point to at least `index + 1` valid `u32`s.
unsafe fn read_nth(ptr: *const u32, index: usize) -> u32 {
    // SAFETY: guaranteed by the caller.
    unsafe { *ptr.add(index) }
}

fn checked_read(values: &[u32], index: usize) -> Option<u32> {
    if index >= values.len() {
        return None;
    }
    // SAFETY: index is in bounds.
    Some(unsafe { read_nth(values.as_ptr(), index) })
}

// Uses pointer offset() in elements and reports 1, not the byte distance.
fn element_stride(values: &[u64]) -> usize {
    if values.len() < 2 {
        return 0;
    }
    // SAFETY: both pointers are inside the same slice.
    unsafe { values.as_ptr().add(1).offset_from(values.as_ptr()) as usize }
}
