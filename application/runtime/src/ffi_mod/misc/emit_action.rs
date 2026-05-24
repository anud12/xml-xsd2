use crate::ffi_mod::runtime_debug_simulate_action;

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
        
        // Check if action exists in cached rows
        let actions = crate::state::last_action_rows().lock().unwrap().clone();
        let mut found = false;
        for row in actions.iter() {
            if row.get(0).map(|s| s.as_str()) == Some(name) {
                found = true;
                break;
            }
        }
        runtime_log!("DEBUG_EMIT: action '{}' {} in cached rows ({} rows)", name, if found { "FOUND" } else { "NOT FOUND" }, actions.len());
        
        // Check if compiled module is available
        let has_compiled = crate::state::get_compiled_module().is_some();
        runtime_log!("DEBUG_EMIT: compiled module {}", if has_compiled { "AVAILABLE" } else { "NOT AVAILABLE" });
    }
    runtime_debug_simulate_action(action_name);
    runtime_log!("DEBUG_EMIT: runtime_emit_action completed");
}
