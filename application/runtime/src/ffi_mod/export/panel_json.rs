use std::ffi::{CStr, CString};
use libc::c_char;

fn parse_id(json: &str) -> Option<String> {
    serde_json::from_str::<serde_json::Value>(json)
        .ok()
        .and_then(|v| v.get("id").and_then(|i| i.as_str()).map(|s| s.to_string()))
}

#[export_name = "runtime_fetch_panel_json"]
pub extern "C" fn runtime_fetch_panel_json(id: *const c_char) -> *mut c_char {
    if id.is_null() { return std::ptr::null_mut(); }
    let c_str = unsafe { CStr::from_ptr(id) };
    let id = match c_str.to_str() { Ok(s) => s, Err(_) => return std::ptr::null_mut() };
    let panels = crate::state::last_panels().lock().unwrap();
    for p in panels.iter() {
        if parse_id(p).as_deref() == Some(id) {
            match CString::new(p.as_str()) {
                Ok(s) => return s.into_raw(),
                Err(_) => return std::ptr::null_mut(),
            }
        }
    }
    std::ptr::null_mut()
}

#[export_name = "runtime_fetch_panel_ids"]
pub extern "C" fn runtime_fetch_panel_ids() -> *mut c_char {
    let panels = crate::state::last_panels().lock().unwrap();
    let ids: Vec<String> = panels.iter()
        .filter_map(|p| parse_id(p))
        .collect();
    match serde_json::to_string(&ids) {
        Ok(json) => match CString::new(json) {
            Ok(s) => s.into_raw(),
            Err(_) => std::ptr::null_mut(),
        },
        Err(_) => std::ptr::null_mut(),
    }
}
