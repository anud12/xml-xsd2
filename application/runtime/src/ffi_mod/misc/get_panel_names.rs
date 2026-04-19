use std::ffi::CString;
use libc::c_char;
use std::ptr;

#[no_mangle]
pub extern "C" fn get_panel_names() -> *mut *mut c_char {
    let panels = crate::state::last_panels().lock().unwrap().clone();
    let mut vec: Vec<*mut c_char> = Vec::new();
    for p in panels.iter() {
        // panels may be JSON strings (serialized objects) or plain ids. Try to extract id from JSON.
        let id = if p.trim_start().starts_with('{') {
            if let Some(pos) = p.find("\"id\"") {
                if let Some(colon) = p[pos..].find(':') {
                    let after = &p[pos + colon + 1..];
                    let mut s = after.trim_start();
                    if s.starts_with('"') {
                        s = &s[1..];
                        if let Some(end) = s.find('"') { s = &s[..end]; }
                    } else {
                        if let Some(end) = s.find(',') { s = &s[..end]; }
                        if let Some(end) = s.find('}') { s = &s[..end]; }
                        s = s.trim();
                    }
                    s.to_string()
                } else { p.clone() }
            } else { p.clone() }
        } else { p.clone() };
        match CString::new(id) {
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