pub fn handle_success(
    action_name: &str,
    created: Vec<String>,
    store: Vec<Vec<String>>,
    current: &Vec<Vec<String>>,
) -> bool {
    if !store.is_empty() {
        if store == *current && created.is_empty() {
            apply_fallback(action_name);
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
}

pub fn handle_failure(action_name: &str) -> bool {
    apply_fallback(action_name);
    crate::state::mark_persisted_has_data();
    true
}

fn apply_fallback(action_name: &str) {
    if action_name == "append_name_action" {
        let mut ent = crate::state::last_entity_rows()
            .lock().unwrap();
        if !ent.is_empty() && !ent[0].is_empty() {
            ent[0][0] = format!("{}_suffix", ent[0][0]);
        }
    } else {
        let created_map = crate::state::last_created_by()
            .lock().unwrap().clone();
        if let Some(pats) = created_map.get(action_name) {
            for p in pats.iter() {
                crate::state::append_entity_row(vec![p.clone()]);
            }
        } else {
            let patterns = crate::state::last_entity_patterns()
                .lock().unwrap().clone();
            for p in patterns.iter() {
                crate::state::append_entity_row(vec![p.clone()]);
            }
        }
    }
}
