#[export_name = "runtime_debug_shutdown"]
pub extern "C" fn runtime_debug_shutdown() {
    debug_println!("debug: shutdown requested");
}
