use std::ffi::{CStr, CString};
use libc::c_char;

/// The name of the action whose plan is currently parked for this actor, or an
/// empty string while the actor is free. The returned C string is allocated
/// with `CString::into_raw` and must be released with `runtime_free_string`.
#[no_mangle]
pub extern "C" fn runtime_get_actor_active_action(
    actor: *const c_char,
) -> *const c_char {
    let name = if actor.is_null() {
        String::new()
    } else {
        let a = unsafe { CStr::from_ptr(actor) }
            .to_string_lossy()
            .trim()
            .to_string();
        crate::state::actor_active_action(&a).unwrap_or_default()
    };
    let c = CString::new(name).unwrap_or_else(|_| CString::new("").unwrap());
    c.into_raw()
}
