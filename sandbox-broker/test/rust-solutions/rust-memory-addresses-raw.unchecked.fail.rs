unsafe fn read_nth(ptr: *const u32, index: usize) -> u32 {
    unsafe { *ptr.add(index) }
}

// No bounds check: an out-of-range index reads past the slice. Undefined behavior.
fn checked_read(values: &[u32], index: usize) -> Option<u32> {
    Some(unsafe { read_nth(values.as_ptr(), index) })
}

fn element_stride(values: &[u64]) -> usize {
    if values.len() < 2 {
        return 0;
    }
    (&values[1] as *const u64 as usize) - (&values[0] as *const u64 as usize)
}
