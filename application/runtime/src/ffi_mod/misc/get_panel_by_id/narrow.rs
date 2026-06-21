use std::ffi::CStr;
use std::ptr;
use libc::c_char;
use super::allocate::allocate_cstr;

// Canonical Cdecl implementation for string-based panel lookup
#[no_mangle]
pub extern "C" fn get_panel_by_id_c(
    id: *const c_char
) -> *mut c_char {
    if id.is_null() { return ptr::null_mut(); }
    let c_str = unsafe { CStr::from_ptr(id) };
    let id_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return ptr::null_mut(),
    };

    let panels = crate::state::last_panels()
        .lock().unwrap().clone();

    for p in panels.iter() {
        if p.trim_start().starts_with('{') {
            if let Some(pos) = p.find("\"id\"") {
                if let Some(colon) = p[pos..].find(':') {
                    let after = &p[pos + colon + 1..];
                    let mut s = after.trim_start();
                    if s.starts_with('"') {
                        s = &s[1..];
                        if let Some(end) = s.find('"') {
                            s = &s[..end];
                        }
                    } else {
                        if let Some(end) = s.find(',') {
                            s = &s[..end];
                        }
                        if let Some(end) = s.find('}') {
                            s = &s[..end];
                        }
                        s = s.trim();
                    }
                    if s == id_str {
                        return allocate_cstr(p);
                    }
                }
            }
        } else if p == &id_str {
            let json = format!(
                "{{\"id\":\"{}\",\"background\":null}}",
                id_str
            );
            return allocate_cstr(&json);
        }
    }

    let json = format!(
        "{{\"id\":\"{}\",\"background\":null}}", id_str
    );
    allocate_cstr(&json)
}

// Backwards-compatible ANSI export for .NET consumers
#[no_mangle]
pub extern "system" fn get_panel_by_id(
    id: *const libc::c_char
) -> *mut libc::c_char {
    get_panel_by_id_c(id)
}
