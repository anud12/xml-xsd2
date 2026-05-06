use std::ffi::{CStr, CString};
use libc::c_char;
use std::ptr;

use serde::Deserialize;

#[derive(Deserialize)]
struct JsPanel {
    id: Option<String>,
    anchor: Option<Anchor>,
    pivot: Option<Anchor>,
    offset: Option<Anchor>,
    size: Option<Size>,
    background: Option<String>,
}

#[derive(Deserialize)]
struct Anchor { x: Option<f32>, y: Option<f32>, top: Option<f32>, bottom: Option<f32>, left: Option<f32>, right: Option<f32> }
#[derive(Deserialize)]
struct Size { height: f32, width: f32 }

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

// Test function: allocate a test string using libc's malloc and return it
// This tests if the FFI pointer mechanism itself is working
#[no_mangle]
pub extern "C" fn test_pointer_return() -> *mut libc::c_char {
    let test_str = CString::new("TEST_POINTER_SUCCESS").unwrap();
    let ptr = test_str.into_raw();
    eprintln!("test_pointer_return: returning ptr={:p}", ptr);
    let _ = std::fs::write("E:\\workspace\\test_pointer_return.txt", format!("test_pointer_return: ptr={:p}, content=TEST_POINTER_SUCCESS\n", ptr));
    ptr
}

// New API: return a pointer to a PanelFfi struct. Caller must free with runtime_free_panel.
#[no_mangle]
pub extern "C" fn get_panel_by_id_struct(id: *const libc::c_char) -> *mut crate::ffi_mod::types::PanelFfi {
    let rust_struct_size = std::mem::size_of::<crate::ffi_mod::types::PanelFfi>();
    let _ = std::fs::write("E:\\workspace\\rust_struct_size.txt", format!("PanelFfi struct size = {} bytes\n", rust_struct_size));
    let _ = std::fs::write("rust_function_called.txt", "get_panel_by_id_struct called\n");
    eprintln!("DEBUG: get_panel_by_id_struct called, PanelFfi struct size = {} bytes", rust_struct_size);
    if id.is_null() { return std::ptr::null_mut(); }
    let id_str = unsafe { CStr::from_ptr(id).to_string_lossy().to_string() };
    
    // DEBUG: test if pointer handling itself works - return a minimal struct with empty JSON
    if id_str == "null_test" {
        eprintln!("DEBUG: Returning null_test struct");
        let children_json_cstr = CString::new("[]").unwrap();
        let panel_json_cstr = CString::new("{}").unwrap();
        let test_panel = Box::new(crate::ffi_mod::types::PanelFfi {
            id: std::ptr::null_mut(),
            background: std::ptr::null_mut(),
            anchor: crate::ffi_mod::types::AnchorFfi { x:0.0, y:0.0 },
            pivot: crate::ffi_mod::types::AnchorFfi { x:0.0, y:0.0 },
            offset: crate::ffi_mod::types::OffsetFfi { top:0.0, bottom:0.0, left:0.0, right:0.0 },
            size: crate::ffi_mod::types::SizeFfi { height:100.0, width:100.0 },
            children_json: children_json_cstr.into_raw(),
            panel_json: panel_json_cstr.into_raw(),
        });
        let raw_ptr = Box::into_raw(test_panel);
        let _ = std::fs::write("E:\\workspace\\rust_struct_verify.txt", format!("null_test: ptr={:p}\n", raw_ptr));
        eprintln!("DEBUG: null_test ptr={:p}", raw_ptr);
        return raw_ptr;
    }
    
    // DEBUG: return a fixed test struct for marshaling diagnostics
    if id_str == "TEST_FIXED" {
        eprintln!("DEBUG: Returning TEST_FIXED struct");
        let id_str = CString::new("TEST_ID").unwrap();
        let bg_str = CString::new("test_bg.png").unwrap();
        let children_str = CString::new("[{\"id\":\"child1\"}]").unwrap();
        let panel_str = CString::new("{\"test\":true}").unwrap();
        
        let test_panel = Box::new(crate::ffi_mod::types::PanelFfi {
            id: id_str.into_raw(),
            background: bg_str.into_raw(),
            anchor: crate::ffi_mod::types::AnchorFfi { x: 1.5, y: 2.5 },
            pivot: crate::ffi_mod::types::AnchorFfi { x: 3.5, y: 4.5 },
            offset: crate::ffi_mod::types::OffsetFfi { top: 5.5, bottom: 6.5, left: 7.5, right: 8.5 },
            size: crate::ffi_mod::types::SizeFfi { height: 100.0, width: 200.0 },
            children_json: children_str.into_raw(),
            panel_json: panel_str.into_raw(),
        });
        let raw_ptr = Box::into_raw(test_panel);
        
        // Log the exact bytes we're returning
        let struct_ptr = raw_ptr as *const u8;
        let struct_bytes = unsafe { std::slice::from_raw_parts(struct_ptr, 72) };
        let hex_str = struct_bytes.iter().map(|b| format!("{:02X}", b)).collect::<Vec<_>>().join(" ");
        
        eprintln!("DEBUG: TEST_FIXED ptr={:p}", raw_ptr);
        eprintln!("DEBUG: TEST_FIXED struct bytes: {}", hex_str);
        
        let _ = std::fs::write("E:\\workspace\\rust_test_fixed_struct.txt", format!(
            "TEST_FIXED returned:\n\
             ptr={:p}\n\
             struct_bytes={}\n",
            raw_ptr, hex_str
        ));
        
        return raw_ptr;
    }
    
    let panels = crate::state::last_panels().lock().unwrap().clone();
    let debug_msg = format!("Looking for id='{}' in {} panels\n", id_str, panels.len());
    // Append to debug file
    std::fs::OpenOptions::new()
        .create(true)
        .append(true)
        .open("E:\\workspace\\rust_debug_complete.txt")
        .ok()
        .and_then(|mut f| {
            use std::io::Write;
            f.write_all(debug_msg.as_bytes()).ok()
        });
    for (i, p) in panels.iter().enumerate() {
        let panel_debug = format!("  Panel {}: {} bytes\n", i, p.len());
        std::fs::OpenOptions::new()
            .create(true)
            .append(true)
            .open("E:\\workspace\\rust_debug_complete.txt")
            .ok()
            .and_then(|mut f| {
                use std::io::Write;
                f.write_all(panel_debug.as_bytes()).ok()
            });
    }

    // Try to find matching JSON panel
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
                        // create PanelFfi from JSON: extract background crude
                        let mut bg_ptr: *mut libc::c_char = std::ptr::null_mut();
                        if let Some(bpos) = p.find("\"background\"") {
                            if let Some(colon2) = p[bpos..].find(':') {
                                let after2 = &p[bpos + colon2 + 1..];
                                let mut sb = after2.trim_start();
                                if sb.starts_with('"') {
                                    sb = &sb[1..];
                                    if let Some(endb) = sb.find('"') { sb = &sb[..endb]; }
                                } else {
                                    if let Some(endb) = sb.find(',') { sb = &sb[..endb]; }
                                    if let Some(endb) = sb.find('}') { sb = &sb[..endb]; }
                                    sb = sb.trim();
                                }
                                if !sb.is_empty() {
                                    let bg_cstr = CString::new(sb.to_string()).unwrap_or_else(|_| CString::new("").unwrap());
                                    bg_ptr = bg_cstr.into_raw();
                                }
                            }
                        }
                        let id_cstr = CString::new(id_str.clone()).unwrap_or_else(|_| CString::new("").unwrap());
                        let id_ptr = id_cstr.into_raw();
                        // parse numeric fields via serde
                        let serde_result = serde_json::from_str::<JsPanel>(p);
                        if let Ok(parsed_panel) = serde_result {
                            let _ = std::fs::write("E:\\workspace\\rust_debug_detail.txt", "Serde parsing succeeded\n");
                            let anchor_raw = parsed_panel.anchor.unwrap_or(Anchor { x: None, y: None, top: None, bottom: None, left: None, right: None });
                            let pivot_raw = parsed_panel.pivot.unwrap_or(Anchor { x: None, y: None, top: None, bottom: None, left: None, right: None });
                            let offset_raw = parsed_panel.offset.unwrap_or(Anchor { x: None, y: None, top: None, bottom: None, left: None, right: None });
                            let size = parsed_panel.size.unwrap_or(Size { height:100.0, width:100.0 });
                            let ax = anchor_raw.x.unwrap_or(0.0);
                            let ay = anchor_raw.y.unwrap_or(0.0);
                            let px = pivot_raw.x.unwrap_or(0.0);
                            let py = pivot_raw.y.unwrap_or(0.0);
                            let ot = offset_raw.top.unwrap_or(offset_raw.y.unwrap_or(0.0));
                            let ob = offset_raw.bottom.unwrap_or(0.0);
                            let ol = offset_raw.left.unwrap_or(offset_raw.x.unwrap_or(0.0));
                            let or = offset_raw.right.unwrap_or(0.0);
                            let sh = size.height;
                            let sw = size.width;
                            // REMOVED OLD LOG LINE FOR DEBUGGING
                            let children_json_ptr = {
                                let v2: serde_json::Value = serde_json::from_str(p).unwrap_or(serde_json::Value::Null);
                                match v2.get("children") {
                                    Some(c) => {
                                        let children_cstr = CString::new(c.to_string()).unwrap_or_else(|_| CString::new("[]").unwrap());
                                        let raw = children_cstr.into_raw();
                                        eprintln!("DEBUG: children from Some, ptr={:p}", raw);
                                        raw
                                    },
                                    None => {
                                        // Always return a non-NULL pointer with empty array
                                        let empty_children_cstr = CString::new("[]").unwrap();
                                        let raw = empty_children_cstr.into_raw();
                                        eprintln!("DEBUG: children from None, ptr={:p}", raw);
                                        raw
                                    }
                                }
                            };
                            let panel_json_cstr = match CString::new(p.as_str()) {
                                Ok(s) => s,
                                Err(_e) => {
                                    // If there's a null byte in the JSON, use a sanitized version
                                    let sanitized = p.chars().filter(|&c| c != '\0').collect::<String>();
                                    CString::new(sanitized).unwrap_or_else(|_| CString::new("{}").unwrap())
                                }
                            };
                            let panel_json_raw = panel_json_cstr.into_raw();
                            eprintln!("DEBUG: panel_json_raw ptr={:p}", panel_json_raw);
                            let _ = std::fs::write("E:\\workspace\\rust_panel_json_ptr.txt", format!("panel_json_raw={:p}, children_ptr={:p}\n", panel_json_raw, children_json_ptr));
                            let children_json_raw = children_json_ptr;
                            let panel = Box::new(crate::ffi_mod::types::PanelFfi {
                                id: id_ptr,
                                background: bg_ptr,
                                anchor: crate::ffi_mod::types::AnchorFfi { x: ax, y: ay },
                                pivot: crate::ffi_mod::types::AnchorFfi { x: px, y: py },
                                offset: crate::ffi_mod::types::OffsetFfi { top: ot, bottom: ob, left: ol, right: or },
                                size: crate::ffi_mod::types::SizeFfi { height: sh, width: sw },
                                children_json: children_json_raw,
                                panel_json: panel_json_raw,
                            });
                            let raw_ptr = Box::into_raw(panel);
                            let _ = std::fs::write("E:\\workspace\\rust_allocated_ptr.txt", format!("Rust allocated: ptr={:p}\n", raw_ptr));
                            // Immediately read back the pointers from the box to verify they're correct
                            let children_read = unsafe { (*raw_ptr).children_json };
                            let panel_read = unsafe { (*raw_ptr).panel_json };
                            let _ = std::fs::write("E:\\workspace\\rust_written_vs_read.txt", format!("WRITTEN: children={:p}, panel={:p}\nREAD_BACK: children={:p}, panel={:p}\n", children_json_raw, panel_json_raw, children_read, panel_read));
                            
                            // Debug: dump the raw bytes of the struct before returning
                            let struct_ptr = raw_ptr as *const u8;
                            let struct_bytes = unsafe { std::slice::from_raw_parts(struct_ptr, std::mem::size_of::<crate::ffi_mod::types::PanelFfi>()) };
                            let hex_str = struct_bytes.iter().map(|b| format!("{:02X}", b)).collect::<Vec<_>>().join(" ");
                            let _ = std::fs::write("E:\\workspace\\rust_struct_dump.txt", format!("Rust struct bytes at ptr={:p}:\n{}\n", raw_ptr, hex_str));
                            
                            eprintln!("MARKER_PATH_SERDE_OK returned ptr {:p}", raw_ptr);
                            return raw_ptr;
                        } else {
                            let panel_json_cstr = match CString::new(p.clone()) {
                                Ok(s) => s,
                                Err(_) => CString::new("{}").expect("Failed to create fallback JSON CString")
                            };
                            let children_json_cstr = CString::new("[]").unwrap();
                            let panel = Box::new(crate::ffi_mod::types::PanelFfi {
                                id: id_ptr,
                                background: bg_ptr,
                                anchor: crate::ffi_mod::types::AnchorFfi { x:0.0, y:0.0 },
                                pivot: crate::ffi_mod::types::AnchorFfi { x:0.0, y:0.0 },
                                offset: crate::ffi_mod::types::OffsetFfi { top:0.0, bottom:0.0, left:0.0, right:0.0 },
                                size: crate::ffi_mod::types::SizeFfi { height:100.0, width:100.0 },
                                children_json: children_json_cstr.into_raw(),
                                panel_json: panel_json_cstr.into_raw(),
                            });
                            eprintln!("MARKER_PATH_SERDE_FAIL returned ptr");
                            return Box::into_raw(panel);
                        }
                    }
                }
            }
        } else {
            if p == &id_str {
                let id_cstr = CString::new(id_str.clone()).unwrap_or_else(|_| CString::new("").unwrap());
                let panel_json_cstr = match CString::new(p.clone()) {
                    Ok(s) => s,
                    Err(_) => CString::new("{}").expect("Failed to create fallback panel_json CString")
                };
                let children_json_cstr = CString::new("[]").unwrap();
                let panel = Box::new(crate::ffi_mod::types::PanelFfi {
                    id: id_cstr.into_raw(),
                    background: std::ptr::null_mut(),
                    anchor: crate::ffi_mod::types::AnchorFfi { x:0.0, y:0.0 },
                    pivot: crate::ffi_mod::types::AnchorFfi { x:0.0, y:0.0 },
                    offset: crate::ffi_mod::types::OffsetFfi { top:0.0, bottom:0.0, left:0.0, right:0.0 },
                    size: crate::ffi_mod::types::SizeFfi { height:100.0, width:100.0 },
                    children_json: children_json_cstr.into_raw(),
                    panel_json: panel_json_cstr.into_raw(),
                });
                eprintln!("MARKER_PATH_FALLBACK returned ptr");
                return Box::into_raw(panel);
            }
        }
    }

    // Not found: return minimal panel with id and empty JSON/children
    let id_cstr = CString::new(id_str.clone()).unwrap_or_else(|_| CString::new("").unwrap());
    let panel_json = format!("{{\"id\":\"{}\"}}", id_str);
    let panel_json_cstr = CString::new(panel_json).unwrap_or_else(|_| CString::new("").unwrap());
    let children_json_cstr = CString::new("[]").unwrap();
    let panel = Box::new(crate::ffi_mod::types::PanelFfi {
        id: id_cstr.into_raw(),
        background: std::ptr::null_mut(),
        anchor: crate::ffi_mod::types::AnchorFfi { x:0.0, y:0.0 },
        pivot: crate::ffi_mod::types::AnchorFfi { x:0.0, y:0.0 },
        offset: crate::ffi_mod::types::OffsetFfi { top:0.0, bottom:0.0, left:0.0, right:0.0 },
        size: crate::ffi_mod::types::SizeFfi { height:100.0, width:100.0 },
        children_json: children_json_cstr.into_raw(),
        panel_json: panel_json_cstr.into_raw(),
    });
    eprintln!("MARKER_PATH_END_OF_FUNC returned ptr");
    Box::into_raw(panel)
}

// Diagnostic test function: return a fixed struct with deterministic values for testing marshaling
#[no_mangle]
pub extern "C" fn get_test_panel_fixed() -> *mut crate::ffi_mod::types::PanelFfi {
    // Create fixed strings with known content
    let id_str = CString::new("TEST_ID").unwrap();
    let bg_str = CString::new("test_bg.png").unwrap();
    let children_str = CString::new("[{\"id\":\"child1\"}]").unwrap();
    let panel_str = CString::new("{\"test\":true}").unwrap();
    
    let id_ptr = id_str.into_raw();
    let bg_ptr = bg_str.into_raw();
    let children_ptr = children_str.into_raw();
    let panel_ptr = panel_str.into_raw();
    
    let test_panel = Box::new(crate::ffi_mod::types::PanelFfi {
        id: id_ptr,
        background: bg_ptr,
        anchor: crate::ffi_mod::types::AnchorFfi { x: 1.5, y: 2.5 },
        pivot: crate::ffi_mod::types::AnchorFfi { x: 3.5, y: 4.5 },
        offset: crate::ffi_mod::types::OffsetFfi { top: 5.5, bottom: 6.5, left: 7.5, right: 8.5 },
        size: crate::ffi_mod::types::SizeFfi { height: 100.0, width: 200.0 },
        children_json: children_ptr,
        panel_json: panel_ptr,
    });
    
    let raw_ptr = Box::into_raw(test_panel);
    
    // Log the exact bytes we're returning
    let struct_ptr = raw_ptr as *const u8;
    let struct_bytes = unsafe { std::slice::from_raw_parts(struct_ptr, 72) };
    let hex_str = struct_bytes.iter().map(|b| format!("{:02X}", b)).collect::<Vec<_>>().join(" ");
    
    eprintln!("get_test_panel_fixed: returning fixed test struct at ptr={:p}", raw_ptr);
    eprintln!("get_test_panel_fixed: struct bytes (72): {}", hex_str);
    eprintln!("get_test_panel_fixed: children_json ptr={:p}, panel_json ptr={:p}", children_ptr, panel_ptr);
    
    let _ = std::fs::write("E:\\workspace\\rust_test_panel_fixed.txt", format!(
        "get_test_panel_fixed returned:\n\
         ptr={:p}\n\
         struct_bytes={}\n\
         id_ptr={:p}\n\
         bg_ptr={:p}\n\
         children_json={:p}\n\
         panel_json={:p}\n",
        raw_ptr, hex_str, id_ptr, bg_ptr, children_ptr, panel_ptr
    ));
    
    raw_ptr
}
