use crate::ffi_mod::{runtime_debug_simulate_action, runtime_debug_simulate_action_args_for};
use std::ffi::CStr;

/// Emits an action bound to an actor. While the actor has a parked plan, the
/// interruptible flag decides: a busy, non-interruptible actor drops the new
/// action (the parked plan is neither interrupted nor queued behind it),
/// whereas a free or interruptible-actor's new action overwrites the parked
/// plan. Mirrors `runtime_emit_action` but is per-actor.
#[no_mangle]
pub extern "C" fn runtime_emit_action_for(
    action_name: *const std::os::raw::c_char,
    actor: *const std::os::raw::c_char,
) {
    if action_name.is_null() {
        return;
    }
    let actor_str = if actor.is_null() {
        String::new()
    } else {
        unsafe { CStr::from_ptr(actor) }
            .to_string_lossy()
            .trim()
            .to_string()
    };
    // A bound actor with a parked, non-interruptible plan rejects the new
    // action outright: the parked plan keeps running untouched (neither
    // interrupted nor queued behind the newcomer). A free or interruptible
    // actor accepts the action, which overwrites the prior plan.
    if !actor_str.is_empty()
        && crate::state::actor_is_busy(&actor_str)
        && !crate::state::actor_plan_interruptible(&actor_str)
    {
        return;
    }
    let _args: Vec<(String, f64)> = Vec::new();
    runtime_debug_simulate_action_args_for(action_name, &_args, &actor_str);
}

#[no_mangle]
pub extern "C" fn runtime_emit_action(action_name: *const std::os::raw::c_char) {
    runtime_log!("DEBUG_EMIT: runtime_emit_action called");
    if action_name.is_null() {
        runtime_log!("DEBUG_EMIT: action_name is null");
        return;
    }
    let c_str = unsafe { std::ffi::CStr::from_ptr(action_name) };
    if let Ok(name) = c_str.to_str() {
        runtime_log!("DEBUG_EMIT: calling with action: {}", name);
    }
    runtime_debug_simulate_action(action_name);
    runtime_log!("DEBUG_EMIT: runtime_emit_action completed");
}
