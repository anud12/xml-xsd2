use std::ffi::CStr;
use libc::c_char;

#[export_name = "runtime_export_state"]
pub extern "C" fn runtime_export_state(path: *const c_char) -> bool {
    if path.is_null() { return false; }
    let c_str = unsafe { CStr::from_ptr(path) };
    match c_str.to_str() {
        Ok(s) => { crate::state::export_to_file(s); true }
        Err(_) => false,
    }
}
