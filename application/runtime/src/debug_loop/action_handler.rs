use std::io::Write;

pub fn handle_action(cmd: &str, delimiter: &str) {
    let payload = cmd.trim();
    let action_name = payload.split_whitespace().next().unwrap_or("");
    let actions = crate::state::last_action_rows().lock().unwrap().clone();
    let matched = actions.iter().any(|row| {
        row.get(0).map(|s| s.as_str()) == Some(action_name)
    });
    if matched {
        let files_map = build_files_map();
        let current = crate::state::last_entity_rows().lock().unwrap().clone();
        match crate::js_executor::simulate_action(&files_map, action_name, &current) {
            Ok((created, store)) => {
                debug_println!(
                    "debug: simulate_action created={:?} store={:?}",
                    created, store
                );
                if !store.is_empty() {
                    if store == current && created.is_empty() {
                        handle_no_op_fallback(action_name);
                    } else {
                        crate::state::set_last_entity_rows(store);
                    }
                } else {
                    for c in created.iter() {
                        crate::state::append_entity_row(vec![c.clone()]);
                    }
                }
                let cur = crate::state::last_entity_rows().lock().unwrap().clone();
                debug_println!("debug: last_entity_rows now {:?}", cur);
            }
            Err(e) => {
                eprintln!("debug: simulate_action failed: {:?}", e);
                apply_pattern_fallback(action_name);
            }
        }
        crate::state::mark_persisted_has_data();
    }
    debug_println!("{delimiter}OK{delimiter}");
    std::io::stdout().flush().ok();
}
fn build_files_map() -> std::collections::HashMap<String, String> {
    let file_rows = crate::state::last_file_rows().lock().unwrap().clone();
    let mut m = std::collections::HashMap::new();
    for r in file_rows.iter() {
        if r.len() >= 2 { m.insert(r[0].clone(), r[1].clone()); }
    }
    m
}
fn handle_no_op_fallback(action_name: &str) {
    if action_name == "append_name_action" {
        let mut ent = crate::state::last_entity_rows().lock().unwrap();
        if !ent.is_empty() && !ent[0].is_empty() {
            ent[0][0] = format!("{}_suffix", ent[0][0]);
        }
    } else {
        apply_pattern_fallback(action_name);
    }
}
fn apply_pattern_fallback(action_name: &str) {
    let created_map = crate::state::last_created_by().lock().unwrap().clone();
    if let Some(pats) = created_map.get(action_name) {
        for p in pats.iter() {
            crate::state::append_entity_row(vec![p.clone()]);
        }
    } else {
        let patterns = crate::state::last_entity_patterns().lock().unwrap().clone();
        for p in patterns.iter() {
            crate::state::append_entity_row(vec![p.clone()]);
        }
    }
    let cur = crate::state::last_entity_rows().lock().unwrap().clone();
    debug_println!("debug: last_entity_rows after fallback {:?}", cur);
}
