use std::ffi::{CStr, CString, c_char, c_int};

unsafe extern "C" {
    fn strlen(text: *const c_char) -> usize;
    fn abs(value: c_int) -> c_int;
}

// unwrap() panics on text with an interior NUL byte instead of returning None.
fn c_strlen(text: &str) -> Option<usize> {
    let owned = CString::new(text).unwrap();
    // SAFETY: owned is NUL-terminated and alive.
    Some(unsafe { strlen(owned.as_ptr()) })
}

// Passes i32::MIN straight to C, where abs(INT_MIN) is undefined behavior.
fn c_abs(value: i32) -> Option<i32> {
    // SAFETY: not actually upheld for i32::MIN.
    Some(unsafe { abs(value) })
}

unsafe fn from_c_string(ptr: *const c_char) -> Option<String> {
    if ptr.is_null() {
        return None;
    }
    // SAFETY: caller guarantees validity.
    Some(unsafe { CStr::from_ptr(ptr) }.to_string_lossy().into_owned())
}
