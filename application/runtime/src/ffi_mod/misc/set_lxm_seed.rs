#[export_name = "runtime_set_seed"]
pub extern "C" fn runtime_set_seed(seed: u32) {
    crate::state::set_lxm_seed(seed);
}
