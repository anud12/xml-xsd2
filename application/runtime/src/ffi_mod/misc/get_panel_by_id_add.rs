// Backwards-compatible ANSI export expected by many .NET consumers (default marshalling)
use libc::c_char;

#[no_mangle]
pub extern "system" fn get_panel_by_id(id: *const c_char) -> *mut c_char {
    // Forward to canonical narrow implementation
    unsafe { crate::ffi_mod::misc::get_panel_by_id_c(id) }
}
