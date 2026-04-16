use std::time::Instant;

#[export_name = "runtime_debug_iterate"]
pub extern "C" fn runtime_debug_iterate(times: u32) {
    for _ in 0..times {
        let start = Instant::now();
        let elapsed = start.elapsed();
        debug_println!("Iteration completed in {{{}:{}}}ns", elapsed.as_secs(), elapsed.subsec_nanos());
    }
}
