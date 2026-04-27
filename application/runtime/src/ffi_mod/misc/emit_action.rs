use crate::ffi_mod::runtime_debug_simulate_action;

#[no_mangle]
pub extern "C" fn runtime_emit_action(action_name: *const std::os::raw::c_char) {
    runtime_debug_simulate_action(action_name);
}
