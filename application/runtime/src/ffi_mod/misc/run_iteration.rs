use std::time::Instant;
use std::collections::HashMap;
use std::sync::atomic::Ordering;

/// Extract effect names from JS source by evaluating it in QuickJS.
/// Returns registered effect names from __registeredEvents.
fn extract_effect_names_from_js(files: &HashMap<String, String>) -> Vec<String> {
    let source = select_entry_source(files);
    if source.is_empty() {
        return Vec::new();
    }

    let rt = match crate::js_runtime::create_runtime() {
        Ok(r) => r,
        Err(_) => return Vec::new(),
    };
    let ctx = match crate::js_runtime::create_context(&rt) {
        Ok(c) => c,
        Err(_) => return Vec::new(),
    };

    if crate::js_host_api::install_host_api(&ctx).is_err() {
        return Vec::new();
    }

    let patched = source
        .replace("({string, number, ...hostApi})", "({...hostApi})")
        .replace("({ string, number, ...hostApi })", "({...hostApi})")
        .replace("({string, ...hostApi})", "({...hostApi})")
        .replace("({number, ...hostApi})", "({...hostApi})");

    let transformed = if patched.contains("export default") {
        patched.replace("export default", "var __module_default =")
    } else {
        patched
    };

    if ctx.with(|c| c.eval::<(), _>(transformed)).is_err() {
        return Vec::new();
    }

    let module_call = include_str!("../../js/scripts/module_call.js");
    if ctx.with(|c| c.eval::<(), _>(module_call)).is_err() {
        return Vec::new();
    }

    let extract_script = r#"
        (function(){
            var evs = globalThis.__registeredEvents || [];
            var names = [];
            for(var i=0; i<evs.length; i++){
                var e = evs[i];
                if(typeof e === "string") names.push(e);
                else if(e && typeof e.name === "string") names.push(e.name);
            }
            return JSON.stringify(names);
        })();
    "#;

    match ctx.with(|c| c.eval::<String, _>(extract_script)) {
        Ok(json) => {
            serde_json::from_str::<Vec<String>>(&json).unwrap_or_default()
        }
        Err(_) => Vec::new(),
    }
}

/// Select the entry source file from the archive files map.
fn select_entry_source(files: &HashMap<String, String>) -> String {
    for (name, content) in files.iter() {
        if name.ends_with("manifest.json") || (name.to_lowercase().contains("manifest") && name.ends_with(".json")) {
            if let Ok(v) = serde_json::from_str::<serde_json::Value>(content) {
                if let Some(entry) = v.get("entry").and_then(|v| v.as_str()) {
                    if let Some(src) = files.get(entry).or_else(|| {
                        if let Some(pos) = name.rfind('/') {
                            let dir = &name[..pos];
                            files.get(&format!("{}/{}", dir, entry))
                        } else { None }
                    }) { return src.clone(); }
                }
            }
        }
    }
    if let Some(src) = files.get("index.js") { return src.clone(); }
    if let Some((_k, v)) = files.iter().next() { return v.clone(); }
    "".to_string()
}

/// Check scheduled effects and add due effects to pending queue.
fn check_scheduled_effects() {
    let game_time = *crate::state::game_time_ms().lock().unwrap();
    let mut scheduled = crate::state::scheduled_effects().lock().unwrap();
    let mut due_effects = Vec::new();
    let mut remaining = Vec::new();
    
    for effect in scheduled.drain(..) {
        if game_time >= effect.scheduled_at_ms {
            due_effects.push(effect.name);
        } else {
            remaining.push(effect);
        }
    }
    
    // Update scheduled effects to only keep non-due ones
    *scheduled = remaining;
    
    // Add due effects to pending queue
    if !due_effects.is_empty() {
        let mut pending = crate::state::pending_effects().lock().unwrap();
        for name in due_effects {
            pending.push(name);
        }
    }
}

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
    runtime_log!("[Runtime] run_iteration start (tick_rate={}s)", tick_rate_in_sec);
    let start = Instant::now();
    
    // Check scheduled effects and add due effects to pending queue
    check_scheduled_effects();

    // Build files map for QuickJS
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    let mut files_map: std::collections::HashMap<String, String> = std::collections::HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            files_map.insert(r[0].clone(), r[1].clone());
        }
    }

  // Auto-queue all effects on first iteration if nothing is pending or scheduled
    {
        // Only auto-queue once per archive processing cycle
        if !crate::state::effects_auto_queued().load(Ordering::SeqCst) {
            let mut pending = crate::state::pending_effects().lock().unwrap();
            let scheduled = crate::state::scheduled_effects().lock().unwrap();
            if pending.is_empty() && scheduled.is_empty() {
                // Try compiled Rust effects first
                let mut queued = false;
                if let Some(compiled) = crate::state::get_compiled_module() {
                    for e in compiled.effects.iter() {
                        pending.push(e.name.clone());
                        queued = true;
                    }
                }
                // Fallback: extract effect names from JS source if compiled module has no effects
                if !queued {
                    let names = extract_effect_names_from_js(&files_map);
                    for name in names {
                        pending.push(name);
                    }
                }
            }
            // Mark that auto-queue has been attempted (regardless of whether effects were queued)
            crate::state::mark_effects_auto_queued();
        }
    }
    
    // Process effects via QuickJS (handles reoccurrence scheduling)
    if let Err(e) = crate::js_executor::process_pending_effects(&files_map) {
        eprintln!("Failed to process pending effects: {:?}", e);
    }
    
    // Re-check scheduled effects - newly scheduled ones from above may also be due
    check_scheduled_effects();
    
    // Process any newly queued effects, looping until no more pending
    let mut max_iterations = 20;
    while max_iterations > 0 {
        let has_pending = !crate::state::pending_effects().lock().unwrap().is_empty();
        if !has_pending { break; }
        max_iterations -= 1;
        
        if let Err(e) = crate::js_executor::process_pending_effects(&files_map) {
            eprintln!("Failed to process pending effects: {:?}", e);
        }
        check_scheduled_effects();
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

    let total = start.elapsed();
    runtime_log!("[Runtime] run_iteration end (elapsed={}ms)", total.as_secs_f64() * 1000.0);
    total.as_secs_f64()
}
