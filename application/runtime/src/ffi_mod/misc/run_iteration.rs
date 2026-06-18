use std::time::Instant;
use std::io::Write;

fn log_debug(msg: &str) {
    eprintln!("{}", msg);
    if let Ok(mut f) = std::fs::OpenOptions::new().create(true).append(true).open("rust_debug_csharp.log") {
        let _ = writeln!(f, "{}", msg);
    }
}

#[no_mangle]
pub extern "C" fn runtime_run_iteration(elapsed_units: i64) -> i64 {
    log_debug(&format!("runtime_run_iteration ENTRY, elapsed_units={}", elapsed_units));
    let start = Instant::now();

    // Add elapsed time units to the cumulative counter FIRST (before processing effects)
    if elapsed_units > 0 {
        crate::state::add_elapsed_time_units(elapsed_units);
    }

    let total = crate::state::get_elapsed_time_units();
    log_debug(&format!("runtime_run_iteration, total elapsed={}", total));

    // Build files map from cached file rows
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    log_debug(&format!("runtime_run_iteration, file_rows count={}", file_rows.len()));
    let mut files_map: std::collections::HashMap<String, String> = std::collections::HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            files_map.insert(r[0].clone(), r[1].clone());
        }
    }
    log_debug(&format!("runtime_run_iteration, files_map count={}", files_map.len()));

    // Debug: check pending effects before processing
    {
        let pe = crate::state::pending_effects().lock().unwrap().clone();
        log_debug(&format!("runtime_run_iteration, pending_effects before processing: {:?}", pe));
    }

    // Debug: check entity number data before processing
    {
        let nd = crate::state::last_entity_number_data().lock().unwrap().clone();
        log_debug(&format!("runtime_run_iteration, entity_number_data before processing: {:?}", nd));
    }

    // Process pending effects (initial execution)
    log_debug("runtime_run_iteration, CALLING process_pending_effects");
    if let Err(e) = crate::js_executor::process_pending_effects(&files_map, total) {
        log_debug(&format!("Failed to process pending effects: {:?}", e));
    }
    log_debug("runtime_run_iteration, RETURNED from process_pending_effects");

    // Debug: check entity number data after processing
    {
        let nd = crate::state::last_entity_number_data().lock().unwrap().clone();
        log_debug(&format!("runtime_run_iteration, entity_number_data after pending_effects: {:?}", nd));
    }

    // Process scheduled reoccurring effects
    log_debug("runtime_run_iteration, CALLING process_scheduled_effects");
    if let Err(e) = crate::js_executor::process_scheduled_effects(&files_map, total) {
        log_debug(&format!("Failed to process scheduled effects: {:?}", e));
    }
    log_debug("runtime_run_iteration, RETURNED from process_scheduled_effects");

    // Debug: check entity number data after all processing
    {
        let nd = crate::state::last_entity_number_data().lock().unwrap().clone();
        log_debug(&format!("runtime_run_iteration, entity_number_data after ALL processing: {:?}", nd));
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
