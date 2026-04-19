#![allow(dead_code)]
use std::collections::HashMap;
use crate::js_executor::extract_from_source;
use crate::js_host_api::Declarations;

pub mod manifest;
pub mod declarations;

/// Converts the zip file map into row tuples for SQLite insertion.
pub fn build_file_rows(files: &HashMap<String, String>) -> Vec<Vec<String>> {
    files.iter().map(|(n, c)| vec![n.clone(), c.clone()]).collect()
}

/// Processes the loaded archive: extracts entities/events from the JS module entry point.
/// Returns entity rows to be persisted.
pub fn process_module(files: &HashMap<String, String>, _entity_rows: &mut Vec<Vec<String>>) {
    // Debug: print available file keys so tests can see what the archive contained.
    eprintln!("process_module: files ({} entries):", files.len());
    for (k, _) in files.iter() { eprintln!("  - {}", k); }

    // Find all manifest files and process each one so a single archive can contain multiple modules.
    let manifests: Vec<(String, serde_json::Value)> = files.iter()
        .filter(|(k, _)| {
            let k_lower = k.to_lowercase();
            k_lower.contains("manifest") && k_lower.contains(".json")
        })
        .filter_map(|(k, s)| serde_json::from_str::<serde_json::Value>(s).ok().map(|v| (k.clone(), v)))
        .collect();

    eprintln!("process_module: found {} manifest(s)", manifests.len());

    if manifests.is_empty() {
        eprintln!("process_module: manifest.json not found");
        eprintln!("process_module: module rejected");
        return;
    }

    for (manifest_name, manifest_json) in manifests {
        eprintln!("process_module: processing manifest {}", manifest_name);
        manifest::set_module_rows(&manifest_json);
        handle_entry_point(&manifest_name, &manifest_json, files);
    }
}

fn handle_entry_point(manifest_name: &str, manifest_json: &serde_json::Value, files: &HashMap<String, String>) {
    let entry_name = manifest::get_entry_name(manifest_json);
    // Try exact match first, then try resolving the entry relative to the manifest's directory.
    if let Some(module_src) = files.get(&entry_name).or_else(|| {
        if let Some(pos) = manifest_name.rfind('/') {
            let dir = &manifest_name[..pos];
            let candidate = format!("{}/{}", dir, entry_name);
            files.get(&candidate)
        } else {
            None
        }
    }) {
        runtime_log!("{} loaded", entry_name);
        manifest::mark_manifest_event(manifest_json);
        process_module_source(module_src);
    } else {
        runtime_log!("Error: entrypoint \"{}\" not found in archive", entry_name);
    }
}

fn process_module_source(module_src: &str) {
    match extract_from_source(module_src) {
        Ok(dec) => declarations::apply_declarations(&dec),
        Err(_) => eprintln!("js extraction failed; no fallback heuristics are used"),
    }
}

#[allow(dead_code)]
fn collect_patterns(dec: &Declarations) -> Vec<String> {
    declarations::collect_patterns(dec)
}

#[allow(dead_code)]
fn build_action_to_created(dec: &Declarations) -> HashMap<String, Vec<String>> {
    declarations::build_action_to_created(dec)
}