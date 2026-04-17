use std::collections::{HashMap, HashSet};
use crate::js_executor::extract_from_source;
use crate::js_host_api::Declarations;

/// Converts the zip file map into row tuples for SQLite insertion.
pub fn build_file_rows(files: &HashMap<String, String>) -> Vec<Vec<String>> {
    files.iter().map(|(n, c)| vec![n.clone(), c.clone()]).collect()
}

/// Locates the manifest JSON in the archive files and parses it.
pub fn find_manifest(files: &HashMap<String, String>) -> Option<(String, serde_json::Value)> {
    // Accept various manifest file naming conventions and path prefixes (case-insensitive)
    files
        .iter()
        .find(|(k, _)| {
            let k_lower = k.to_lowercase();
            // match any filename or path that mentions "manifest" and has a json extension
            k_lower.contains("manifest") && k_lower.contains(".json")
        })
        .and_then(|(k, s)| {
            serde_json::from_str::<serde_json::Value>(s)
                .ok()
                .map(|v| (k.clone(), v))
        })
}

/// Prints event declarations to stdout and returns the set of seen event names.
pub fn print_events_from_declarations(dec: &Declarations) -> HashSet<String> {
    let mut seen = HashSet::new();
    for ev in dec.events.iter() {
        runtime_log!("event: {}", ev);
        runtime_log!("event registered: {}", ev);
        seen.insert(ev.clone());
    }
    seen
}

/// Processes the loaded archive: extracts entities/events from the JS module entry point.
/// Returns entity rows to be persisted.
pub fn process_module(
    files: &HashMap<String, String>,
    entity_rows: &mut Vec<Vec<String>>,
) {
    match find_manifest(files) {
        Some((manifest_name, manifest_json)) => {
            runtime_log!("module process: found manifest {}", manifest_name);
            runtime_log!("{} loaded", manifest_name);
            if let Some(id_v) = manifest_json.get("id").and_then(|v| v.as_str()) {
                let name_v = manifest_json.get("name").and_then(|v| v.as_str()).unwrap_or("");
                let version_v = manifest_json.get("version").and_then(|v| v.as_str()).unwrap_or("");
                crate::state::set_last_module_rows(vec![vec![id_v.to_string(), name_v.to_string(), version_v.to_string()]]);
            }
            if let Some(entry) = manifest_json.get("entry").and_then(|v| v.as_str()) {
                if let Some(module) = files.get(entry) {
                    runtime_log!("{} loaded", entry);
                    if let Some(evt_name) =
                        manifest_json.get("eventName").and_then(|v| v.as_str())
                    {
                        runtime_log!("event: {}", evt_name);
                        runtime_log!("event registered: {}", evt_name);
                        // mark that module declared events/entities so export should include persisted DB
                        runtime_log!("module process: manifest has eventName, marking persisted_has_data");
                        crate::state::mark_persisted_has_data();
                    }
                    if let Ok(dec) = extract_from_source(module) {
                        runtime_log!("module process: extract_from_source succeeded");
                        crate::state::mark_persisted_has_data();
                        runtime_log!("module process: marked persisted_has_data after extract");
                        // Print effects/events
                        print_events_from_declarations(&dec);
                        // Print actions
                        for action in dec.actions.iter() {
                            runtime_log!("action: {}", action);
                            runtime_log!("action registered: {}", action);
                        }
                        for l in dec.logs.iter() {
                            runtime_log!("{}", l);
                        }
                        // Debug: print creators/emits mapping discovered from module
                        runtime_log!("creators: {:?}", dec.creators);
                        runtime_log!("emits: {:?}", dec.emits);
                        // Prefer creators mapping (action/effect -> created entity names) when available
                        let mut patterns: Vec<String> = Vec::new();
                        for (_k, v) in dec.creators.iter() {
                            for item in v.iter() {
                                if !patterns.contains(item) {
                                    patterns.push(item.clone());
                                }
                            }
                        }
                        // Fallback to any loose createdEntities discovered
                        for en in dec.entities.iter() {
                            if !patterns.contains(en) {
                                patterns.push(en.clone());
                            }
                        }
                        crate::state::set_last_entity_patterns(patterns);

                        // Record action and event declarations for export
                        let action_rows: Vec<Vec<String>> = dec.actions.iter().map(|a| vec![a.clone()]).collect();
                        crate::state::set_last_action_rows(action_rows);
                        let event_rows: Vec<Vec<String>> = dec.events.iter().map(|e| vec![e.clone()]).collect();
                        crate::state::set_last_event_rows(event_rows);

                        // Build mapping from action name -> created entity patterns by inspecting
                        // declarations: creators (effect/action -> created patterns) and emits (action -> emitted effect names).
                        let mut action_to_created: HashMap<String, Vec<String>> = HashMap::new();
                        // Include creators keyed by action name directly
                        for (k, v) in dec.creators.iter() {
                            if dec.actions.iter().any(|a| a == k) {
                                action_to_created.insert(k.clone(), v.clone());
                            }
                        }
                        // Use emits mapping: action -> emitted effect names -> look up creators for those effects
                        for (action, emitted) in dec.emits.iter() {
                            if dec.actions.iter().any(|a| a == action) {
                                let mut patterns: Vec<String> = Vec::new();
                                for e_name in emitted.iter() {
                                    if let Some(pats) = dec.creators.get(e_name) {
                                        patterns.extend(pats.clone());
                                    }
                                }
                                if !patterns.is_empty() {
                                    action_to_created.insert(action.clone(), patterns);
                                }
                            }
                        }
                        // Store mapping in state for use by debug loop when ACTION is invoked.
                        crate::state::set_last_created_by(action_to_created);
                    } else {
                        eprintln!("js extraction failed; no fallback heuristics are used");
                    }
                } else {
                    eprintln!("entry {} not found in files", entry);
                }
            }
        }
        None => {
            runtime_log!("manifest.json not found");
            // dump available file keys to aid debugging
            for (k, _) in files.iter() {
                runtime_log!("file present: {}", k);
            }
            runtime_log!("module rejected");
        }
    }
}
