use crate::ffi_mod::debug::debug_simulate_action::{
    runtime_debug_simulate_action, runtime_debug_simulate_action_args,
};
use crate::ffi_mod::debug::debug_simulate_action::runtime_debug_simulate_action_args_for;
use libc::c_char;
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

/// Mirrors the C# `ActionArgs` struct: `count` + two parallel pointers
/// (`keys`: NUL-terminated C strings, `values`: f64). The pairs are positional
/// (key[i] pairs with value[i]) and delivered to the action's `ctx.args`.
#[no_mangle]
pub extern "C" fn runtime_emit_action_args(
    action_name: *const std::os::raw::c_char,
    count: *const i32,
    keys: *const *const c_char,
    values: *const f64,
) {
    if action_name.is_null() {
        return;
    }
    let n = unsafe { std::ptr::read(count) };
    if n <= 0 || keys.is_null() || values.is_null() {
        // No args: fall back to the plain emit.
        runtime_emit_action(action_name);
        return;
    }
    let mut pairs: Vec<(String, f64)> = Vec::with_capacity(n as usize);
    for i in 0..n {
        let idx = (i as usize).try_into().unwrap();
        let kptr = unsafe { std::ptr::read(keys.add(idx)) };
        let key = if kptr.is_null() {
            String::new()
        } else {
            unsafe { CStr::from_ptr(kptr) }.to_string_lossy().into_owned()
        };
        let val = unsafe { std::ptr::read(values.add(idx)) };
        pairs.push((key, val));
    }
    runtime_debug_simulate_action_args(action_name, &pairs);
}

/// Publishes action args into the persistent sim context (so the action's
/// `ctx.args` / the effect's `globalThis.__actionArgs` sees them) and queues
/// `effect_name` to run on the next `runtime_run_iteration`. This mirrors the
/// real `.ui` click flow: `emitAction` publishes `__actionArgs`, the action's
/// `emitEffect` enqueues the effect by name, and `RunIteration`'s
/// `process_pending_effects` fires it against the persistent context (where
/// `__actionArgs` and the synced `__containerData` live).
#[no_mangle]
pub extern "C" fn runtime_publish_action_args_and_queue_effect(
    action_name: *const std::os::raw::c_char,
    effect_name: *const std::os::raw::c_char,
    count: *const i32,
    keys: *const *const c_char,
    values: *const f64,
) {
    let n = if count.is_null() { 0 } else { unsafe { std::ptr::read(count) } };
    if n <= 0 || keys.is_null() || values.is_null() {
        return;
    }
    let mut pairs: Vec<(String, f64)> = Vec::with_capacity(n as usize);
    for i in 0..n {
        let idx = (i as usize).try_into().unwrap();
        let kptr = unsafe { std::ptr::read(keys.add(idx)) };
        let key = if kptr.is_null() {
            String::new()
        } else {
            unsafe { CStr::from_ptr(kptr) }.to_string_lossy().into_owned()
        };
        let val = unsafe { std::ptr::read(values.add(idx)) };
        pairs.push((key, val));
    }
    // Publish `__actionArgs` into the persistent sim context (no-op if no
    // module is installed), then queue the effect by name. The action paths
    // queue effects by name, so the args are also stashed as the effect's
    // typed `prepare(context, input)` input (the payload must travel
    // separately from the name-only pending queue).
    crate::ffi_mod::debug::debug_simulate_action::runtime_debug_simulate_action_args(
        std::ptr::null_mut(), &pairs,
    );
    if !effect_name.is_null() {
        if let Ok(name) = unsafe { CStr::from_ptr(effect_name) }.to_str() {
            let trimmed = name.trim();
            crate::state::pending_effects()
                .lock()
                .unwrap()
                .push(trimmed.to_string());
            let mut map = serde_json::Map::new();
            for (k, v) in pairs.iter() {
                map.insert(k.clone(), serde_json::Value::from(*v));
            }
            crate::js_executor::sim_ctx::stash_effect_input(
                trimmed,
                serde_json::Value::Object(map),
            );
        }
    }
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
