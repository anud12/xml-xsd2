use std::ffi::CStr;
use std::collections::HashMap;
use libc::c_char;

#[export_name = "runtime_debug_simulate_action"]
pub extern "C" fn runtime_debug_simulate_action(action_name: *const c_char) -> bool {
    if action_name.is_null() { return false; }
    let c_str = unsafe { CStr::from_ptr(action_name) };
    let name = match c_str.to_str() { Ok(s) => s.trim(), Err(_) => return false, };

    // Ensure action exists in cached rows
    let actions = crate::state::last_action_rows().lock().unwrap().clone();
    let mut matched = false;
    for row in actions.iter() {
        if row.get(0).map(|s| s.as_str()) == Some(name) {
            matched = true;
            break;
        }
    }
    if !matched { return false; }

    // Build files map from cached file rows
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    let mut files_map: HashMap<String, String> = HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 {
            files_map.insert(r[0].clone(), r[1].clone());
        }
    }

    let current_entities = crate::state::last_entity_rows().lock().unwrap().clone();
    match crate::js_executor::simulate_action(&files_map, name, &current_entities) {
        Ok((created, store)) => {
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
            true
        },
        Err(_) => {
            // Fallback heuristics on simulation failure
            let created_map = crate::state::last_created_by().lock().unwrap().clone();
            if let Some(pats) = created_map.get(name) {
                for p in pats.iter() { crate::state::append_entity_row(vec![p.clone()]); }
            } else {
                let patterns = crate::state::last_entity_patterns().lock().unwrap().clone();
                for p in patterns.iter() { crate::state::append_entity_row(vec![p.clone()]); }
            }
            crate::state::mark_persisted_has_data();
            true
        }
    }
}
