//! FFI transport seam for the Room/Portal world state (Layer A).
//!
//! The C# client pulls the room/portal model through this function.
//! Follows the existing c_char pointer + runtime_free_string convention.

use libc::c_char;
use std::ffi::CString;

fn to_c_string(s: String) -> *mut c_char {
    match CString::new(s) {
        Ok(cs) => cs.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}

/// Rooms + portals as JSON (for the RTS world view):
/// `{"rooms":[...],"portals":[...]}`.
#[no_mangle]
pub extern "C" fn runtime_fetch_world_state() -> *mut c_char {
    to_c_string(crate::state::fetch_rooms_json())
}
