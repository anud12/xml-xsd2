use std::time::Instant;

#[no_mangle]
pub extern "C" fn runtime_run_iteration(elapsed_units: i64) -> i64 {
    let start = Instant::now();

    // Add elapsed time units to the cumulative counter FIRST (before processing effects)
    if elapsed_units > 0 {
        crate::state::add_elapsed_time_units(elapsed_units);
    }

    let total = crate::state::get_elapsed_time_units();

    // Build files map from cached file rows
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    let mut files_map: std::collections::HashMap<String, String> = std::collections::HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            files_map.insert(r[0].clone(), r[1].clone());
        }
    }

    // Debug: check pending effects before processing
    {
        let pe = crate::state::pending_effects().lock().unwrap().clone();
        eprintln!("DEBUG: run_iteration, pending_effects before processing: {:?}", pe);
    }

    // Process pending effects (initial execution)
    if let Err(e) = crate::js_executor::process_pending_effects(&files_map, total) {
        eprintln!("Failed to process pending effects: {:?}", e);
    }

    // Process scheduled reoccurring effects
    if let Err(e) = crate::js_executor::process_scheduled_effects(&files_map, total) {
        eprintln!("Failed to process scheduled effects: {:?}", e);
    }

    let _elapsed = start.elapsed();

    total
}

#[no_mangle]
pub extern "C" fn runtime_emit_event(name_ptr: *const std::ffi::c_char) {
    let name = unsafe {
        if name_ptr.is_null() {
            String::new()
        } else {
            std::ffi::CStr::from_ptr(name_ptr).to_string_lossy().into_owned()
        }
    };
    crate::state::pending_effects().lock().unwrap().push(name);
}

#[no_mangle]
pub extern "C" fn runtime_get_elapsed_time_units() -> i64 {
    crate::state::get_elapsed_time_units()
}
