#![allow(dead_code)]
use std::collections::HashMap;

pub mod manifest;
pub mod declarations;
mod entry;

/// Converts the zip file map into row tuples for SQLite insertion.
pub fn build_file_rows(
    files: &HashMap<String, String>,
) -> Vec<Vec<String>> {
    files.iter()
        .map(|(n, c)| vec![n.clone(), c.clone()])
        .collect()
}

/// Processes the loaded archive: extracts entities/events from the JS module
/// entry point. Returns entity rows to be persisted.
pub fn process_module(
    files: &HashMap<String, String>,
    _entity_rows: &mut Vec<Vec<String>>,
) {
    eprintln!("process_module: files ({} entries):", files.len());
    for (k, _) in files.iter() { eprintln!("  - {}", k); }

    let manifests: Vec<(String, serde_json::Value)> = files.iter()
        .filter(|(k, _)| {
            let k_lower = k.to_lowercase();
            k_lower.contains("manifest") && k_lower.contains(".json")
        })
        .filter_map(|(k, s)| serde_json::from_str::<serde_json::Value>(s)
            .ok().map(|v| (k.clone(), v)))
        .collect();

    eprintln!("process_module: found {} manifest(s)", manifests.len());

    if manifests.is_empty() {
        eprintln!("process_module: manifest.json not found");
        eprintln!("process_module: module rejected");
        return;
    }

    for (manifest_name, manifest_json) in manifests {
        eprintln!("process_module: processing manifest {}",
            manifest_name);
        manifest::set_module_rows(&manifest_json);
        entry::handle_entry_point(
            &manifest_name, &manifest_json, files);
    }
}
