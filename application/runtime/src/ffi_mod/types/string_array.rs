use std::ffi::CString;
use libc::c_char;

/// Convert a Vec<String> into a C-compatible null-terminated string array.
pub unsafe fn string_vec_to_c_array(
    vec: Vec<String>,
) -> (*mut *mut c_char, usize) {
    if vec.is_empty() {
        return (std::ptr::null_mut(), 0);
    }
    let v: Vec<*mut c_char> = vec
        .into_iter()
        .map(|s| {
            let c = CString::new(s).unwrap_or_else(
                |_| CString::new("").unwrap(),
            );
            c.into_raw()
        })
        .collect();
    let len = v.len();
    let boxed = v.into_boxed_slice();
    let ptr = Box::into_raw(boxed) as *mut *mut c_char;
    (ptr, len)
}

/// Free a C string array previously allocated by string_vec_to_c_array.
pub unsafe fn free_c_string_array(
    ptr: *mut *mut c_char,
    len: usize,
) {
    if ptr.is_null() || len == 0 {
        return;
    }
    let slice = std::ptr::slice_from_raw_parts_mut(ptr, len);
    let boxed: Box<[*mut c_char]> = Box::from_raw(slice);
    for &p in boxed.iter() {
        if !p.is_null() {
            let _ = CString::from_raw(p);
        }
    }
}
