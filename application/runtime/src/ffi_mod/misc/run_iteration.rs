use std::time::Instant;

#[no_mangle]
pub extern "C" fn runtime_run_iteration(tick_rate_in_sec: f64) -> f64 {
    let start = Instant::now();
    
    // Build files map from cached file rows
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    let mut files_map: std::collections::HashMap<String, String> = std::collections::HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            files_map.insert(r[0].clone(), r[1].clone());
        }
    }
    
    // Process pending effects
    if let Err(e) = crate::js_executor::process_pending_effects(&files_map) {
        eprintln!("Failed to process pending effects: {:?}", e);
    }
    
    let elapsed = start.elapsed();
    
    // If tickRateInSec is 0, return immediately with elapsed time
    // If tickRateInSec > 0, wait until at least that duration has passed
    if tick_rate_in_sec > 0.0 {
        let tick_duration = std::time::Duration::from_secs_f64(tick_rate_in_sec);
        if elapsed < tick_duration {
            let remaining = tick_duration - elapsed;
            std::thread::sleep(remaining);
        }
    }
    
    start.elapsed().as_secs_f64()
}
