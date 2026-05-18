use std::time::Instant;

/// Try to process pending effects via the Rust execution engine.
/// Falls back to QuickJS if no compiled module is available.
fn try_rust_effect_processing() -> bool {
    let compiled = match crate::state::get_compiled_module() {
        Some(c) => c,
        None => return false,
    };
    let pending = crate::state::pending_effects().lock().unwrap().clone();
    if pending.is_empty() {
        return true; // nothing to do
    }
    crate::state::clear_pending_effects();

    let text_data = crate::state::last_entity_data().lock().unwrap().clone();
    let number_data_f64 = crate::state::last_entity_number_data().lock().unwrap().clone();
    let number_data: std::collections::HashMap<String, std::collections::HashMap<String, i64>> = number_data_f64.into_iter()
        .map(|(id, map)| (id, map.into_iter().map(|(k, v)| (k, v as i64)).collect()))
        .collect();

    for effect_name in pending {
        runtime_log!("DEBUG: Rust processing effect '{}'", effect_name);
        let result = match crate::module::execution::execute_effect(
            &compiled,
            &effect_name,
            1,
            "system",
            &text_data,
            &number_data,
        ) {
            Ok(r) => r,
            Err(e) => {
                runtime_log!("DEBUG: Rust effect execution failed: {}", e);
                return false;
            }
        };

        // Apply text mutations
        for (eid, key, val) in result.text_mutations {
            let mut data = crate::state::last_entity_data().lock().unwrap();
            data.entry(eid.clone()).or_default().insert(key, val);
        }
        // Apply number mutations
        for (eid, key, val) in result.number_mutations {
            let mut data = crate::state::last_entity_number_data().lock().unwrap();
            data.entry(eid.clone()).or_default().insert(key, val as f64);
        }
        // Forward logs
        for log in result.logs {
            runtime_log!("DEBUG: {}", log);
        }
        // Re-queue any emitted effects
        if !result.emitted_effects.is_empty() {
            let mut pe = crate::state::pending_effects().lock().unwrap();
            for name in result.emitted_effects {
                pe.push(name);
            }
        }
    }
    true
}

#[no_mangle]
pub extern "C" fn runtime_run_iteration(tick_rate_in_sec: f64) -> f64 {
    let start = Instant::now();

    // Try Rust path first; fall back to QuickJS
    if !try_rust_effect_processing() {
        // Build files map from cached file rows
        let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
        let mut files_map: std::collections::HashMap<String, String> = std::collections::HashMap::new();
        for r in file_rows.iter() {
            if r.len() >= 2 {
                files_map.insert(r[0].clone(), r[1].clone());
            }
        }
        if let Err(e) = crate::js_executor::process_pending_effects(&files_map) {
            eprintln!("Failed to process pending effects: {:?}", e);
        }
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
