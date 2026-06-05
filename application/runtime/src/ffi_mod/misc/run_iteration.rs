use std::time::Instant;

#[no_mangle]
pub extern "C" fn runtime_run_iteration(elapsed_units: i64) -> i64 {
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

    // Add elapsed time units to the cumulative counter
    if elapsed_units > 0 {
        crate::state::add_elapsed_time_units(elapsed_units);
    }

    let total = crate::state::get_elapsed_time_units();
    let _elapsed = start.elapsed();

    total
}

#[no_mangle]
pub extern "C" fn runtime_get_elapsed_time_units() -> i64 {
    crate::state::get_elapsed_time_units()
}
