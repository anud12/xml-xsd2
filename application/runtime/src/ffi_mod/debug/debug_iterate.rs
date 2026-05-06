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
    use std::collections::HashMap;
    
    // Build files map from cached file rows
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    let mut files_map: HashMap<String, String> = HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            files_map.insert(r[0].clone(), r[1].clone());
        }
    }

    // Log pending effects count to file for debugging
    let pe_count = crate::state::pending_effects().lock().unwrap().len();
    use std::io::Write;
    if let Ok(mut f) = std::fs::OpenOptions::new().create(true).append(true).open("C:\\temp\\rust_debug.log") {
        let _ = writeln!(f, "[{}] runtime_debug_iterate: pending_effects_count={}", std::process::id(), pe_count);
    }

    // Call the effect processor
    if let Err(e) = crate::js_executor::process_pending_effects(&files_map) {
        eprintln!("Failed to process pending effects: {:?}", e);
    }

    // Log result state after processing
    let nd = crate::state::last_entity_number_data().lock().unwrap();
    if let Ok(mut f) = std::fs::OpenOptions::new().create(true).append(true).open("C:\\temp\\rust_debug.log") {
        let _ = writeln!(f, "[{}] after process_pending_effects: number_data={:?}", std::process::id(), nd);
    }
}
