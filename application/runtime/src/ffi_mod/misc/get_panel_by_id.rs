use std::ffi::{CStr, CString};
use libc::c_char;
use std::ptr;

// Canonical Cdecl implementation (internal name to avoid duplicate exports)
#[no_mangle]
pub extern "C" fn get_panel_by_id_c(id: *const c_char) -> *mut c_char {
    use std::os::raw::c_void;
    extern "system" {
        fn CoTaskMemAlloc(cb: usize) -> *mut c_void;
    }

    if id.is_null() { return ptr::null_mut(); }
    let c_str = unsafe { CStr::from_ptr(id) };
    let id_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return ptr::null_mut(),
    };

    // Look up panels cache; panels entries may be JSON objects or plain ids.
    let panels = crate::state::last_panels().lock().unwrap().clone();

    // Try to find a JSON panel whose id matches the requested id
    for p in panels.iter() {
        if p.trim_start().starts_with('{') {
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
                    if s == id_str {
                        // Return the full JSON object string so client can parse id/background etc.
                        let out = p.clone();
                        unsafe {
                            let bytes = std::ffi::CString::new(out).unwrap_or_else(|_| std::ffi::CString::new("").unwrap());
                            let len = bytes.to_bytes_with_nul().len();
                            let mem = CoTaskMemAlloc(len) as *mut i8;
                            if mem.is_null() { return ptr::null_mut(); }
                            std::ptr::copy_nonoverlapping(bytes.as_ptr() as *const i8, mem, len);
                            return mem as *mut c_char;
                        }
                    }
                }
            }
        } else {
            if p == &id_str {
                // plain id match; return a minimal JSON object with id and no background
                let json = format!("{{\"id\":\"{}\",\"background\":null}}", id_str);
                unsafe {
                    let bytes = std::ffi::CString::new(json).unwrap_or_else(|_| std::ffi::CString::new("").unwrap());
                    let len = bytes.to_bytes_with_nul().len();
                    let mem = CoTaskMemAlloc(len) as *mut i8;
                    if mem.is_null() { return ptr::null_mut(); }
                    std::ptr::copy_nonoverlapping(bytes.as_ptr() as *const i8, mem, len);
                    return mem as *mut c_char;
                }
            }
        }
    }

    // Not found: return JSON with id and null background
    let json = format!("{{\"id\":\"{}\",\"background\":null}}", id_str);
    unsafe {
        let bytes = std::ffi::CString::new(json).unwrap_or_else(|_| std::ffi::CString::new("").unwrap());
        let len = bytes.to_bytes_with_nul().len();
        let mem = CoTaskMemAlloc(len) as *mut i8;
        if mem.is_null() { return ptr::null_mut(); }
        std::ptr::copy_nonoverlapping(bytes.as_ptr() as *const i8, mem, len);
        mem as *mut c_char
    }
}

// Provide a StdCall (Windows "system") export named exactly `get_panel_by_id` for .NET consumers
// Attempt a UTF-16 (wide) signature to match DllImport(CharSet=Unicode) marshaling on Windows.
#[export_name = "get_panel_by_id_wide"]
pub extern "system" fn get_panel_by_id_wide(id: *const u16) -> *mut u16 {

    use std::slice;
    use std::os::windows::ffi::{OsStringExt, OsStrExt};
    use std::ffi::OsString;
    use std::ptr;
    use std::os::raw::c_void;
    #[link(name = "ole32")]
    extern "system" {
        fn CoTaskMemAlloc(cb: usize) -> *mut c_void;
    }

    if id.is_null() { return ptr::null_mut(); }
    unsafe {
        // Determine length and convert input wide string to Rust String
        let mut len = 0usize;
        while *id.add(len) != 0 { len += 1; }
        let slice = slice::from_raw_parts(id, len);
        let os = OsString::from_wide(slice);
        let id_str = os.to_string_lossy().into_owned();

        // Call existing narrow implementation
        use std::ffi::CString;
        let c_input = CString::new(id_str.clone()).unwrap_or_else(|_| CString::new("").unwrap());
        let res_ptr = get_panel_by_id_c(c_input.as_ptr());
        if res_ptr.is_null() { return ptr::null_mut(); }
        let res_cstr = std::ffi::CStr::from_ptr(res_ptr);
        let res_str = res_cstr.to_string_lossy().into_owned();
        // free the original c string memory
        crate::ffi_mod::runtime_free_string(res_ptr);

        // Encode to UTF-16 and allocate memory using CoTaskMemAlloc so .NET can free it safely
        let mut wide: Vec<u16> = OsString::from(res_str).encode_wide().collect();
        // ensure null terminator
        wide.push(0);
        let byte_len = wide.len() * std::mem::size_of::<u16>();
        let mem = CoTaskMemAlloc(byte_len) as *mut u16;
        if mem.is_null() { return ptr::null_mut(); }
        ptr::copy_nonoverlapping(wide.as_ptr(), mem, wide.len());
        mem
    }
}

// Backwards-compatible ANSI export expected by many .NET consumers (default marshalling)
#[no_mangle]
pub extern "system" fn get_panel_by_id(id: *const libc::c_char) -> *mut libc::c_char {
    // Forward to canonical narrow implementation
    get_panel_by_id_c(id)
}
