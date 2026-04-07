use std::collections::{HashMap, HashSet};
use crate::js_executor::extract_from_source;
use crate::js_host_api::Declarations;

/// Converts the zip file map into row tuples for SQLite insertion.
pub fn build_file_rows(files: &HashMap<String, String>) -> Vec<Vec<String>> {
    files.iter().map(|(n, c)| vec![n.clone(), c.clone()]).collect()
}

/// Locates the manifest JSON in the archive files and parses it.
pub fn find_manifest(files: &HashMap<String, String>) -> Option<(String, serde_json::Value)> {
    if files.contains_key("manifest.json") {
        let name = "manifest.json".to_string();
        files
            .get(&name)
            .and_then(|s| serde_json::from_str::<serde_json::Value>(s).ok())
            .map(|v| (name, v))
    } else {
        files
            .iter()
            .find(|(k, _)| k.starts_with("manifest") && k.ends_with(".json"))
            .and_then(|(k, s)| {
                serde_json::from_str::<serde_json::Value>(s)
                    .ok()
                    .map(|v| (k.clone(), v))
            })
    }
}

/// Prints event declarations to stdout and returns the set of seen event names.
pub fn print_events_from_declarations(dec: &Declarations) -> HashSet<String> {
    let mut seen = HashSet::new();
    for ev in dec.events.iter() {
        println!("event: {}", ev);
        println!("event registered: {}", ev);
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
            println!("{} loaded", manifest_name);
            if let Some(entry) = manifest_json.get("entry").and_then(|v| v.as_str()) {
                if let Some(module) = files.get(entry) {
                    println!("{} loaded", entry);
                    if let Some(evt_name) =
                        manifest_json.get("eventName").and_then(|v| v.as_str())
                    {
                        println!("event: {}", evt_name);
                        println!("event registered: {}", evt_name);
                        // mark that module declared events/entities so export should include persisted DB
                        crate::state::mark_persisted_has_data();
                    }
                    if let Ok(dec) = extract_from_source(module) {
                        crate::state::mark_persisted_has_data();
                        print_events_from_declarations(&dec);
                        for l in dec.logs.iter() {
                            println!("{}", l);
                        }
                        for en in dec.entities.iter() {
                            entity_rows.push(vec![format!("{},", en)]);
                        }
                    } else {
                        eprintln!("js extraction failed; no fallback heuristics are used");
                    }
                } else {
                    eprintln!("entry {} not found in files", entry);
                }
            }
        }
        None => {
            println!("manifest.json not found");
            println!("module rejected");
        }
    }
}
