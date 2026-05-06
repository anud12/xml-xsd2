use std::collections::HashMap;
use crate::js_host_api::Declarations;

pub fn print_events_from_declarations(dec: &Declarations) -> std::collections::HashSet<String> {
    let mut seen = std::collections::HashSet::new();
    for ev in dec.events.iter() {
        runtime_log!("event: {}", ev);
        runtime_log!("event registered: {}", ev);
        seen.insert(ev.clone());
    }
    seen
}

pub fn apply_declarations(dec: &Declarations) {
    runtime_log!("module process: extract_from_source succeeded");
    crate::state::mark_persisted_has_data();
    print_events_from_declarations(dec);
    for action in dec.actions.iter() { runtime_log!("action: {}", action); runtime_log!("action registered: {}", action); }
    for l in dec.logs.iter() { runtime_log!("{}", l); }
    runtime_log!("creators: {:?}", dec.creators);
    runtime_log!("emits: {:?}", dec.emits);
    let patterns = collect_patterns(dec);
    crate::state::set_last_entity_patterns(patterns);
    // Append discovered panels to the cached last_panels so multiple modules processed in one archive
    // contribute cumulatively rather than overwriting the cache.
    {
        let mut existing = crate::state::last_panels().lock().unwrap();
        for p in dec.panels.iter() {
            if !existing.contains(p) { existing.push(p.clone()); }
        }
    }
    for p in dec.panels.iter() { runtime_log!("panel: {}", p); }
    crate::state::set_last_action_rows(dec.actions.iter().map(|a| vec![a.clone()]).collect());
    crate::state::set_last_event_rows(dec.events.iter().map(|e| vec![e.clone()]).collect());
    let action_map = build_action_to_created(dec);
    crate::state::set_last_created_by(action_map);
    // Store entity textMap data from setEntity calls
    if let serde_json::Value::Object(entities) = &dec.entity_data {
        let mut data: std::collections::HashMap<String, std::collections::HashMap<String, String>> = std::collections::HashMap::new();
        let mut number_data: std::collections::HashMap<String, std::collections::HashMap<String, f64>> = std::collections::HashMap::new();
        for (entity_id, entity_val) in entities {
            if let Some(text_map) = entity_val.get("textMap").and_then(|v| v.as_object()) {
                let mut tm: std::collections::HashMap<String, String> = std::collections::HashMap::new();
                for (k, v) in text_map {
                    if let Some(s) = v.as_str() {
                        tm.insert(k.clone(), s.to_string());
                    }
                }
                data.insert(entity_id.clone(), tm);
            }
            if let Some(number_map) = entity_val.get("numberMap").and_then(|v| v.as_object()) {
                let mut nm: std::collections::HashMap<String, f64> = std::collections::HashMap::new();
                for (k, v) in number_map {
                    if let Some(n) = v.as_f64() {
                        nm.insert(k.clone(), n);
                    }
                }
                number_data.insert(entity_id.clone(), nm);
            }
        }
        crate::state::set_last_entity_data(data);
        crate::state::set_last_entity_number_data(number_data);
        // Collect entity IDs from setEntity calls (keys of entity_data) to add as entity rows
        let entity_ids_from_set: Vec<String> = entities.keys().cloned().collect();

        // Append entities from both createEntity() and setEntity() to last_entity_rows
        append_entity_rows(dec, &entity_ids_from_set);
    } else {
        // Even when there's no entity_data, still add entities created via createEntity()
        append_entity_rows(dec, &[]);
    }
}

/// Append entity rows to the cached last_entity_rows.
/// Collects entities from both dec.entities (createEntity calls) and set_entity_ids (setEntity keys).
fn append_entity_rows(dec: &Declarations, set_entity_ids: &[String]) {
    let mut existing = crate::state::last_entity_rows().lock().unwrap();

    // Add entities from createEntity() calls
    for en in dec.entities.iter() {
        if !row_exists(&existing, en) {
            existing.push(vec![en.clone()]);
        }
    }

    // Add entity IDs from setEntity() calls (these may not appear in dec.entities)
    for eid in set_entity_ids {
        if !row_exists(&existing, eid) {
            existing.push(vec![eid.clone()]);
        }
    }
}

/// Check if an entity row with the given name already exists.
fn row_exists(rows: &[Vec<String>], name: &str) -> bool {
    for row in rows.iter() {
        if let Some(first) = row.first() {
            if first == name { return true; }
        }
    }
    false
}

pub fn collect_patterns(dec: &Declarations) -> Vec<String> {
    let mut patterns: Vec<String> = Vec::new();
    add_creator_patterns(&mut patterns, dec);
    add_entity_patterns(&mut patterns, dec);
    patterns
}

fn add_creator_patterns(patterns: &mut Vec<String>, dec: &Declarations) {
    for (_k, v) in dec.creators.iter() {
        for item in v.iter() { add_unique(patterns, item.clone()); }
    }
}

fn add_entity_patterns(patterns: &mut Vec<String>, dec: &Declarations) {
    for en in dec.entities.iter() { add_unique(patterns, en.clone()); }
}

fn add_unique(vec: &mut Vec<String>, item: String) {
    if !vec.contains(&item) { vec.push(item); }
}

pub fn build_action_to_created(dec: &Declarations) -> HashMap<String, Vec<String>> {
    let mut action_to_created: HashMap<String, Vec<String>> = HashMap::new();
    insert_creator_actions(&mut action_to_created, dec);
    insert_emitted_actions(&mut action_to_created, dec);
    action_to_created
}

fn insert_creator_actions(map: &mut HashMap<String, Vec<String>>, dec: &Declarations) {
    for (k, v) in dec.creators.iter() {
        if dec.actions.iter().any(|a| a == k) { map.insert(k.clone(), v.clone()); }
    }
}

fn insert_emitted_actions(map: &mut HashMap<String, Vec<String>>, dec: &Declarations) {
    for (action, emitted) in dec.emits.iter() {
        if !dec.actions.iter().any(|a| a == action) { continue; }
        let mut pats: Vec<String> = Vec::new();
        for e_name in emitted.iter() { if let Some(p) = dec.creators.get(e_name) { pats.extend(p.clone()); } }
        if !pats.is_empty() { map.insert(action.clone(), pats); }
    }
}