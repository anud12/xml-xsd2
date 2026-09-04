use std::os::raw::c_char;

use crate::ffi_mod::runtime_debug_simulate_action;
use crate::ffi_mod::debug::runtime_debug_simulate_action_args;

#[no_mangle]
pub extern "C" fn runtime_emit_action(action_name: *const c_char) {
    if action_name.is_null() {
        return;
    }
    runtime_debug_simulate_action(action_name);
}

/// Fixed-layout block of action args: `count` NUL-terminated key strings
/// followed by `count` f64 values, in one contiguous allocation. The producer
/// (C#) owns the block for the duration of the call; keys[i] names values[i].
#[repr(C)]
pub struct ActionArgs {
    pub count: usize,
    pub keys: *const *const c_char,
    pub values: *const f64,
}

impl ActionArgs {
    pub fn to_pairs(&self) -> Vec<(String, f64)> {
        if self.keys.is_null() || self.values.is_null() || self.count == 0 {
            return Vec::new();
        }
        let keys = unsafe { std::slice::from_raw_parts(self.keys, self.count) };
        let vals = unsafe { std::slice::from_raw_parts(self.values, self.count) };
        let mut out = Vec::with_capacity(self.count);
        for (k, v) in keys.iter().zip(vals.iter()) {
            if k.is_null() { continue; }
            let first = unsafe { **k };
            if first == 0 { continue; }
            let key = unsafe { std::ffi::CStr::from_ptr(*k) }
                .to_string_lossy().into_owned();
            out.push((key, *v));
        }
        out
    }
}

/// Like `runtime_emit_action`, but carries an args payload (key/value list)
/// delivered to the action's `ctx.args`.
#[no_mangle]
pub extern "C" fn runtime_emit_action_args(
    action_name: *const c_char,
    args: *const ActionArgs,
) {
    if action_name.is_null() {
        return;
    }
    let pairs = if args.is_null() {
        Vec::new()
    } else {
        unsafe { &*args }.to_pairs()
    };
    runtime_debug_simulate_action_args(action_name, &pairs);
}

/// Like `runtime_emit_action`, but binds the action to an actor (entity id).
/// Used to enforce per-actor serialization while an action plan is parked.
#[no_mangle]
pub extern "C" fn runtime_emit_action_for(
    action_name: *const c_char,
    actor: *const c_char,
) {
    use crate::ffi_mod::debug::runtime_debug_simulate_action_for;
    runtime_debug_simulate_action_for(action_name, actor);
}
