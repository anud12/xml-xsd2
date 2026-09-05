use std::ffi::CStr;
use libc::{c_char, c_int};

/// True while the actor has a parked action plan (busy). A busy actor is the
/// one that would drop (or take over) a further action; a free actor is not
/// "doing anything". Returns a 4-byte c_int (0/1): a Rust `bool` is 1 byte and
/// would leave the upper bytes of the return register undefined, which C# would
/// misread as non-zero.
#[no_mangle]
pub extern "C" fn runtime_is_actor_busy(
    actor: *const c_char,
) -> c_int {
    if actor.is_null() {
        return 0;
    }
    let actor = unsafe { CStr::from_ptr(actor) }
        .to_string_lossy()
        .trim()
        .to_string();
    if crate::state::actor_is_busy(&actor) { 1 } else { 0 }
}
