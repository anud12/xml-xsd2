use crate::ffi_mod::runtime_debug_simulate_action;

#[no_mangle]
pub extern "C" fn runtime_emit_action(action_name: *const std::os::raw::c_char) {
    runtime_log!("DEBUG_EMIT: runtime_emit_action called");
    
    use std::io::Write;
    let c_str_opt = if action_name.is_null() {
        None
    } else {
        Some(unsafe { std::ffi::CStr::from_ptr(action_name) })
    };
    if let Some(c_str) = c_str_opt {
        if let Ok(name) = c_str.to_str() {
            if let Ok(mut f) = std::fs::OpenOptions::new().create(true).append(true).open("C:\\temp\\rust_debug.log") {
                let _ = writeln!(f, "[{}] runtime_emit_action: action={}", std::process::id(), name);
            }
        }
    }
    
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

/// Like `runtime_emit_action`, but binds the action to an actor (entity id).
/// Used to enforce per-actor serialization while an action plan is parked.
#[no_mangle]
pub extern "C" fn runtime_emit_action_for(
    action_name: *const std::os::raw::c_char,
    actor: *const std::os::raw::c_char,
) {
    runtime_log!(
        "DEBUG_EMIT: runtime_emit_action_for called (actor ptr null={})",
        actor.is_null());
    use crate::ffi_mod::debug::runtime_debug_simulate_action_for;
    runtime_debug_simulate_action_for(action_name, actor);
    runtime_log!("DEBUG_EMIT: runtime_emit_action_for completed");
}
