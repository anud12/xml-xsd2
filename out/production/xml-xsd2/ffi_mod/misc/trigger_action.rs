use libc::c_char;

#[export_name = "trigger_action"]
pub extern "C" fn trigger_action(action_name: *const c_char) -> bool {
    // Delegate to debug simulate implementation which mirrors the runtime ACTION handling
    crate::ffi_mod::debug::runtime_debug_simulate_action(action_name)
}
