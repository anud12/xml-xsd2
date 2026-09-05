use std::ffi::CStr;
use libc::{c_char, c_int};

/// True while the actor's parked plan was marked interruptible (allowInterrupt()).
/// A busy, interruptible actor accepts a new action; otherwise the action is dropped.
/// Returns a 4-byte c_int (0/1): a Rust `bool` is 1 byte and would leave the upper
/// bytes of the return register undefined, which C# would misread as non-zero.
#[no_mangle]
pub extern "C" fn runtime_is_actor_interruptible(
    actor: *const c_char,
) -> c_int {
    if actor.is_null() {
        return 0;
    }
    let actor = unsafe { CStr::from_ptr(actor) }
        .to_string_lossy()
        .trim()
        .to_string();
    if crate::state::actor_plan_interruptible(&actor) { 1 } else { 0 }
}
