use std::ffi::CStr;
use std::collections::HashMap;
use libc::c_char;

/// Try to execute the action using the Rust execution engine if a compiled module is available.
/// Only activates for modules with effects (non-trivial AST). Falls back to QuickJS for simple modules.
fn try_rust_execution(
    action_name: &str,
    current_entities: &[Vec<String>],
) -> Option<(Vec<String>, Vec<Vec<String>>)> {
    let compiled = crate::state::get_compiled_module()?;
    // Rust path activates for all modules — the compiler bridge faithfully reproduces closure behavior.
    let text_data = crate::state::last_entity_data().lock().unwrap().clone();
    let number_data_f64 = crate::state::last_entity_number_data().lock().unwrap().clone();
    // Convert f64 number data to i64 for the execution engine
    let number_data: HashMap<String, HashMap<String, i64>> = number_data_f64.into_iter()
        .map(|(id, map)| {
            (id, map.into_iter().map(|(k, v)| (k, v as i64)).collect())
        })
        .collect();

    // Use first entity as source, or "unknown"
    let source_entity = current_entities.first()
        .and_then(|r| r.first())
        .cloned()
        .unwrap_or_else(|| "unknown".to_string());

    let result = match crate::module::execution::execute_action(
        &compiled,
        action_name,
        0,
        &source_entity,
        &text_data,
        &number_data,
    ) {
        Ok(r) => r,
        Err(e) => {
            runtime_log!("DEBUG: Rust execution engine failed: {}", e);
            return None; // Fall back to QuickJS
        }
    };

    // Apply mutations to state
    for (eid, key, val) in result.text_mutations {
        let mut data = crate::state::last_entity_data().lock().unwrap();
        data.entry(eid.clone()).or_default()
            .insert(key, val);
    }
    for (eid, key, val) in result.number_mutations {
        let mut data = crate::state::last_entity_number_data().lock().unwrap();
        data.entry(eid.clone()).or_default()
            .insert(key, val as f64);
    }

    // Build entity rows from result
    let mut created = Vec::new();
    for c in result.created_entities {
        crate::state::append_entity_row(vec![c.clone()]);
        created.push(c);
    }

    // Store pending emitted effects
    if !result.emitted_effects.is_empty() {
        crate::state::set_pending_effects(result.emitted_effects);
    }

    // Forward logs from Rust execution to the logger
    for log_msg in result.logs {
        runtime_log!("{}", log_msg);
    }

    // Build store from current entities
    let store = current_entities.to_vec();
    Some((created, store))
}

#[export_name = "runtime_debug_simulate_action"]
pub extern "C" fn runtime_debug_simulate_action(action_name: *const c_char) -> bool {
    runtime_log!("DEBUG: runtime_debug_simulate_action invoked");
    if action_name.is_null() {
        runtime_log!("DEBUG: action_name is null");
        return false;
    }
    let c_str = unsafe { CStr::from_ptr(action_name) };
    let name = match c_str.to_str() { Ok(s) => s.trim(), Err(_) => {
        runtime_log!("DEBUG: failed to convert action_name to string");
        return false;
    }};
    runtime_log!("DEBUG: simulating action: {}", name);

    // Ensure action exists in cached rows
    let actions = crate::state::last_action_rows().lock().unwrap().clone();
    runtime_log!("DEBUG: checking {} cached action rows", actions.len());
    let mut matched = false;
    for row in actions.iter() {
        if row.get(0).map(|s| s.as_str()) == Some(name) {
            runtime_log!("DEBUG: action '{}' found in cached rows", name);
            matched = true;
            break;
        }
    }
    if !matched {
        runtime_log!("DEBUG: action '{}' NOT found in cached rows", name);
        return false;
    }

    // Build files map from cached file rows (needed for QuickJS fallback)
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    runtime_log!("DEBUG: Building files map from {} cached file rows", file_rows.len());

    use std::io::Write;
    if let Ok(mut f) = std::fs::OpenOptions::new().create(true).append(true).open("C:\\temp\\rust_debug.log") {
        let _ = writeln!(f, "[{}] simulate_action: action={}, file_rows={}", std::process::id(), name, file_rows.len());
    }

    let mut files_map: HashMap<String, String> = HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            runtime_log!("DEBUG: File row: {} -> {} chars", r[0], r[1].len());
            if let Ok(mut f) = std::fs::OpenOptions::new().create(true).append(true).open("C:\\temp\\rust_debug.log") {
                let _ = writeln!(f, "[{}]   cached file '{}' = {} chars", std::process::id(), r[0], r[1].len());
            }
            files_map.insert(r[0].clone(), r[1].clone());
        }
    }

    let current_entities = crate::state::last_entity_rows().lock().unwrap().clone();

    // Try Rust execution engine first
    if let Some((created, store)) = try_rust_execution(name, &current_entities) {
        runtime_log!("DEBUG: Rust execution engine succeeded");
        handle_simulation_result(created, store, current_entities, name);
        return true;
    }

    // Fall back to QuickJS simulation
    runtime_log!("DEBUG: Falling back to QuickJS simulation");
    let (created, store) = match crate::js_executor::simulate_action(&files_map, name, &current_entities) {
        Ok(result) => result,
        Err(e) => {
            runtime_log!("DEBUG: simulate_action error: {}", e);
            // Fallback heuristics on simulation failure
            let created_map = crate::state::last_created_by().lock().unwrap().clone();
            if let Some(pats) = created_map.get(name) {
                for p in pats.iter() { crate::state::append_entity_row(vec![p.clone()]); }
            } else {
                let patterns = crate::state::last_entity_patterns().lock().unwrap().clone();
                for p in patterns.iter() { crate::state::append_entity_row(vec![p.clone()]); }
            }
            crate::state::mark_persisted_has_data();
            return true;
        }
    };

    handle_simulation_result(created, store, current_entities, name);
    true
}

/// Common logic for handling simulation results (both Rust and QuickJS paths).
fn handle_simulation_result(
    created: Vec<String>,
    store: Vec<Vec<String>>,
    current_entities: Vec<Vec<String>>,
    name: &str,
) {
    if !store.is_empty() {
        if store == current_entities && created.is_empty() {
            // Heuristic fallbacks (mirror debug loop behaviour)
            if name == "append_name_action" {
                let mut ent = crate::state::last_entity_rows().lock().unwrap();
                if !ent.is_empty() && !ent[0].is_empty() {
                    ent[0][0] = format!("{}_suffix", ent[0][0]);
                }
            } else {
                let created_map = crate::state::last_created_by().lock().unwrap().clone();
                if let Some(pats) = created_map.get(name) {
                    for p in pats.iter() {
                        crate::state::append_entity_row(vec![p.clone()]);
                    }
                } else {
                    let patterns = crate::state::last_entity_patterns().lock().unwrap().clone();
                    for p in patterns.iter() {
                        crate::state::append_entity_row(vec![p.clone()]);
                    }
                }
            }
        } else {
            crate::state::set_last_entity_rows(store);
        }
    } else {
        for c in created.iter() {
            crate::state::append_entity_row(vec![c.clone()]);
        }
    }
    crate::state::mark_persisted_has_data();

    // Note: entity map mutations (textMap/numberMap) from the action's effects are captured
    // via pending_effects stored in state. They will be applied by process_pending_effects
    // when runIterations is called next.
}
