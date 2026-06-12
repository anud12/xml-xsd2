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
    // Store pending effects from emitEvent calls during module processing
    if !dec.pending_effects.is_empty() {
        eprintln!("DEBUG: apply_declarations setting pending_effects: {:?}", dec.pending_effects);
        runtime_log!("pending effects: {:?}", dec.pending_effects);
        crate::state::set_pending_effects(dec.pending_effects.clone());
        // Verify the state was set
        let verify = crate::state::pending_effects().lock().unwrap().clone();
        eprintln!("DEBUG: after set_pending_effects, pending_effects is: {:?}", verify);
    } else {
        eprintln!("DEBUG: apply_declarations, pending_effects is empty!");
    }

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
    }
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