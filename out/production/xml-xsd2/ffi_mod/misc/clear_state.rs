#[export_name = "runtime_clear_state"]
pub extern "C" fn runtime_clear_state() {
    crate::state::clear_state();
}
