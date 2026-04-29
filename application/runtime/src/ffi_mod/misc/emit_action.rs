use crate::ffi_mod::runtime_debug_simulate_action;

#[no_mangle]
pub extern "C" fn runtime_emit_action(action_name: *const std::os::raw::c_char) {
    runtime_log!("DEBUG_EMIT: runtime_emit_action called");
    
    use std::io::Write;
    if let Ok(c_str) = (if action_name.is_null() { Err(()) } else { Ok(unsafe { std::ffi::CStr::from_ptr(action_name) }) }) {
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
