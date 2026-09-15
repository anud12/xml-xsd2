//! FFI transport seam for the registered action list.
//!
//! The C# client pulls the action ids through this function.
//! Follows the existing c_char pointer + runtime_free_string convention.

use libc::c_char;
use std::ffi::CString;

fn to_c_string(s: String) -> *mut c_char {
    match CString::new(s) {
        Ok(cs) => cs.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}

/// Registered action ids as a JSON array of strings, e.g. `["increment","attack"]`.
#[no_mangle]
pub extern "C" fn runtime_fetch_action_ids() -> *mut c_char {
    let rows = crate::state::last_action_rows().lock().unwrap();
    let ids: Vec<&str> = rows.iter().map(|r| r.first().map(|s| s.as_str()).unwrap_or_default()).collect();
    to_c_string(serde_json::to_string(&ids).unwrap_or_else(|_| "[]".to_string()))
}
