use std::ffi::CString;
use libc::c_char;
use std::ptr;

#[no_mangle]
pub extern "C" fn get_panel_names() -> *mut *mut c_char {
    let panels = crate::state::last_panels().lock().unwrap().clone();
    let mut vec: Vec<*mut c_char> = Vec::new();
    for p in panels.iter() {
        match CString::new(p.clone()) {
            Ok(c) => vec.push(c.into_raw()),
            Err(_) => vec.push(CString::new("").unwrap().into_raw()),
        }
    }
    // Null-terminate the array as some consumers expect it
    vec.push(ptr::null_mut());
    if vec.is_empty() {
        return ptr::null_mut();
    }
    let boxed = vec.into_boxed_slice();
    Box::into_raw(boxed) as *mut *mut c_char
}

// Backwards-compatible alias expected by some clients (C#/other):
#[no_mangle]
pub extern "C" fn get_panel_ids() -> *mut *mut c_char {
    // Forward to the canonical implementation
    get_panel_names()
}