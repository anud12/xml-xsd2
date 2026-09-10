use std::time::Instant;

#[export_name = "runtime_debug_iterate"]
pub extern "C" fn runtime_debug_iterate(times: u32) {
    for _i in 0..times {
        let _start = Instant::now();

        // Process pending effects
        process_pending_effects();
    }
}

fn process_pending_effects() {
    let current_elapsed = crate::state::get_elapsed_time_units();
    if let Err(e) = crate::js_executor::process_pending_effects(current_elapsed) {
        eprintln!("Failed to process pending effects: {:?}", e);
    }
}
