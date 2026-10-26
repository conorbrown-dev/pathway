use std::ffi::{CStr, CString, c_char, c_int};

unsafe extern "C" {
    fn strlen(text: *const c_char) -> usize;
    fn abs(value: c_int) -> c_int;
}

fn c_strlen(text: &str) -> Option<usize> {
    let owned = CString::new(text).ok()?;
    // SAFETY: `owned` is a valid NUL-terminated string that lives until the end of this function.
    Some(unsafe { strlen(owned.as_ptr()) })
}

fn c_abs(value: i32) -> Option<i32> {
    if value == i32::MIN {
        // abs(INT_MIN) is undefined behavior in C: the result doesn't fit in an int.
        return None;
    }
    // SAFETY: abs has no preconditions for values other than INT_MIN.
    Some(unsafe { abs(value) })
}

/// # Safety
/// `ptr` must be null or point to a NUL-terminated string that stays valid for the call.
unsafe fn from_c_string(ptr: *const c_char) -> Option<String> {
    if ptr.is_null() {
        return None;
    }
    // SAFETY: non-null, and the caller guarantees NUL termination and validity.
    let text = unsafe { CStr::from_ptr(ptr) };
    text.to_str().ok().map(String::from)
}
