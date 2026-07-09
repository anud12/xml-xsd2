use std::ffi::CString;
use libc::c_char;
use std::ptr;

#[no_mangle]
pub extern "C" fn get_container_ids() -> *mut *mut c_char {
    let containers =
        crate::state::last_containers().lock().unwrap().clone();
    let mut vec: Vec<*mut c_char> = Vec::new();
    for json_str in containers.iter() {
        let id = extract_container_id(json_str);
        match CString::new(id) {
            Ok(c) => vec.push(c.into_raw()),
            Err(_) => vec.push(CString::new("").unwrap().into_raw()),
        }
    }
    vec.push(ptr::null_mut());
    if vec.is_empty() {
        return ptr::null_mut();
    }
    let boxed = vec.into_boxed_slice();
    Box::into_raw(boxed) as *mut *mut c_char
}

fn extract_container_id(json_str: &str) -> String {
    let trimmed = json_str.trim();
    if trimmed.starts_with('{') {
        if let Some(pos) = trimmed.find("\"id\"") {
            if let Some(colon) = trimmed[pos..].find(':') {
                let after = &trimmed[pos + colon + 1..];
                let mut s = after.trim_start();
                if s.starts_with('"') {
                    s = &s[1..];
                    if let Some(end) = s.find('"') {
                        s = &s[..end];
                    }
                } else {
                    if let Some(end) = s.find(',') { s = &s[..end]; }
                    if let Some(end) = s.find('}') { s = &s[..end]; }
                    s = s.trim();
                }
                return s.to_string();
            }
        }
    }
    trimmed.to_string()
}

#[no_mangle]
pub extern "C" fn get_container_by_id(
    id: *const c_char,
) -> *mut c_char {
    if id.is_null() {
        return CString::new("").unwrap().into_raw();
    }
    let id_str = unsafe {
        std::ffi::CStr::from_ptr(id).to_string_lossy().to_string()
    };
    let containers =
        crate::state::last_containers().lock().unwrap();
    for json_str in containers.iter() {
        let cid = extract_container_id(json_str);
        if cid == id_str {
            return CString::new(json_str.as_str())
                .unwrap_or_else(|_|
                    CString::new("").unwrap()
                )
                .into_raw();
        }
    }
    ptr::null_mut()
}

#[no_mangle]
pub extern "C" fn runtime_free_container(p: *mut c_char) {
    if !p.is_null() {
        unsafe {
            let _ = CString::from_raw(p);
        }
    }
}
