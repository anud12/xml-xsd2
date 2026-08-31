//! FFI transport seam for the .ui layer.
//!
//! The C# client pulls the UI tree / id-keyed deltas through these
//! functions. Follows the existing c_char pointer + runtime_free_string
//! convention.

use libc::c_char;
use std::ffi::CString;

fn to_c_string(s: String) -> *mut c_char {
    match CString::new(s) {
        Ok(cs) => cs.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}

/// Full UI tree as JSON (for initial paint):
/// `{"nodes":[...],"moduleOwners":{...}}`.
#[no_mangle]
pub extern "C" fn runtime_fetch_ui_state() -> *mut c_char {
    crate::ui::tick();
    to_c_string(crate::ui::fetch_ui_state_json())
}

/// True when an id-keyed UI delta is pending since the last fetch.
#[no_mangle]
pub extern "C" fn runtime_ui_dirty() -> bool {
    crate::ui::tick();
    crate::ui::ui_dirty().load(std::sync::atomic::Ordering::SeqCst)
}

/// Pending delta as JSON (`{"ops":[{"op":"add"|"update","node":{...}},
/// {"op":"remove","id":"..."}]}`), or null when clean. Clears the dirty flag.
#[no_mangle]
pub extern "C" fn runtime_fetch_ui_delta() -> *mut c_char {
    crate::ui::tick();
    let dirty = crate::ui::ui_dirty();
    if !dirty.load(std::sync::atomic::Ordering::SeqCst) {
        return std::ptr::null_mut();
    }
    let json = crate::ui::fetch_ui_delta_json();
    dirty.store(false, std::sync::atomic::Ordering::SeqCst);
    match json {
        Some(j) => to_c_string(j),
        None => std::ptr::null_mut(),
    }
}

/// Registered animation definitions as JSON (`{name: {frames:[...]}}`).
/// Consumers advance frames themselves using the elapsed time units.
#[no_mangle]
pub extern "C" fn runtime_fetch_ui_animations() -> *mut c_char {
    let map = crate::ui::animations().lock().unwrap().clone();
    to_c_string(serde_json::to_string(&map).unwrap_or_default())
}
